
using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;
using CBIZ.CCH.BatchExtension.Application.Features.GFRObjects;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text;
using System.Text.Json;



namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;

internal class GfrService : IGfrService
{
    private readonly ILogger<GfrService> _logger;
    private readonly GfrEndPointOptions _gfrEndPointsOptions;
    private readonly GfrApiAccessInfo _GfrApiAccessInfo;    
    private readonly ApiHelper _apiHelper;
    private readonly ProcessOptions _processOptions;

    GfrAuthResponse token = GfrAuthResponse.Empty;
    
    public GfrService(
        ILogger<GfrService> logger,
        IOptions<GfrEndPointOptions> gfrEndPointsOptions,
        IOptions<GfrApiAccessInfo> GfrApiAccessInfo,
        IOptions<ProcessOptions> processOptions,
        ApiHelper apiHelper

    )
    {
        _logger = logger;
        _gfrEndPointsOptions = gfrEndPointsOptions.Value;
        _GfrApiAccessInfo = GfrApiAccessInfo.Value;    
        _processOptions =  processOptions.Value; 
        _apiHelper = apiHelper;
    }

    private async Task BuildClientAuth(bool refreshTicket, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In BuildClientAuth");
        if (refreshTicket) token = GfrAuthResponse.Empty;       
        if (token.IsEmptyToken)
        {
            
            var header = ApiHelper.CreateHeader(
                token: string.Empty,
                appIdKey: _GfrApiAccessInfo.ApiAppId,
                contentType: ApiHelper.ContentTypeAppJson,
                ApiHelper.TokenTypeBasic
            );

            var url = $"{ApiURL(_gfrEndPointsOptions.Auth)}";
            //var request = new AuthorizationRequest(_GfrApiAccessInfo.UserName, _GfrApiAccessInfo.Password);           
            var request = new AuthorizationRequest("gfrserviceaccount2@cbiz.com" , "huf%jbXY~@)2V#B");           
            var jsonRequestBody = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, ApiHelper.ContentTypeAppJson);
            var response = await _apiHelper.ExecutePostAsync(url, jsonRequestBody, header, cancellationToken);            
            if (response.HasFailure)
            {
                _logger.LogError(response.Failure,"Error getting Authentication {Failure}", response.Failure);
                await Task.FromException(new BatchExtensionException("Error getting Authentication"));
            }
            var final = await _apiHelper.DeserializeResult<GfrAuthResponse>(response.Value);
            if (final.HasFailure)
            {
                _logger.LogError(final.Failure,"Error getting Authentication {Failure}", final.Failure);
                await Task.FromException(new BatchExtensionException("Error getting Authentication"));
            }

            token = final.Value;
            
        }
    }

    public async Task<Possible<BatchExtensionException>> UploadDocumentToGfr(
           GfrDocument gfrDocument,
           IBatchService batchService,
           BatchExtensionData document,
           bool refreshGfrTicket,
           CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In UploadDocumentToGfr");

        await BuildClientAuth(refreshGfrTicket, cancellationToken);

        var indexResponse = await GetIndexesByDrawerIdAsync(cancellationToken);
        if (indexResponse.HasFailure)
        {
            _logger.LogError(indexResponse.Failure, "Error getting Indexes :{Message}", indexResponse.Failure.Message);
            return new BatchExtensionException($"Error getting Indexes :{indexResponse.Failure.Message}");
        }

        
        var clientsResponse = await GetClientsByDrawerAsync(cancellationToken);
        if(clientsResponse.HasFailure)
        {
            _logger.LogError(clientsResponse.Failure, "Error getting Gfr clients : {Message}", clientsResponse.Failure.Message);
            return new BatchExtensionException($"Error getting Gfr clients {clientsResponse.Failure.Message}");  
        }
        
        var filteredClient = clientsResponse.Value.FirstOrDefault(_ => string.Equals(_.ClientNumber, gfrDocument.clientNumber, StringComparison.OrdinalIgnoreCase));
        if (filteredClient == null)
        {
            _logger.LogError( "Error finding client for : {ClientNumber}:{ClientName}", gfrDocument.clientNumber, gfrDocument.clientName);
            return new BatchExtensionException($"Error finding client for  {gfrDocument.clientNumber}:{gfrDocument.clientName}"); 
        }

        var newDocumentId = await CreateDocumentAsync(indexResponse.Value, gfrDocument, cancellationToken);
        if (newDocumentId.HasFailure)
        {
            _logger.LogError(indexResponse.Failure, "Error getting Indexes :{Message}", indexResponse.Failure.Message);
            return new BatchExtensionException($"Error creating GFR document {newDocumentId.Failure.Message}");
        } else if (string.IsNullOrWhiteSpace(newDocumentId.Value)){
            _logger.LogError("Error getting Indexes: newDocumentId is blank");
            return new BatchExtensionException($"Error getting Indexes: newDocumentId is blank");
        }

        //Update db with the new documentId
        await batchService.UpdateBatchItemUpdateGfrDocumentId(document.BatchItemGuid, newDocumentId.Value, cancellationToken);        
        var returnFileName = Path.Combine( _processOptions.DownloadFilesDirectory,  gfrDocument.fileName);
        var fileBytes = await File.ReadAllBytesAsync(returnFileName, cancellationToken);
        var uploadResponse = await UploadDocumentAsync(
            newDocumentId.Value,
            fileBytes,
            gfrDocument.fileName,
            cancellationToken);
        
        HelperFunctions.DeleteFile(returnFileName);
        
        if(uploadResponse.HasFailure)
        {
             _logger.LogError(indexResponse.Failure, "Error Uploading GFR document {Message}", uploadResponse.Failure.Message);
            return new BatchExtensionException($"Error Uploading GFR document {uploadResponse.Failure.Message}");
        }

        return Possible.Completed;
        
    }

    
    public async Task<Possible<BatchExtensionException>> UpdateWorkFlowRoute(
        string firmFlowId, 
        IBatchService batchService, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating route for firmflow Id: {ID} in UpdateWorkFlowRoute", firmFlowId);

        try
        {
            await BuildClientAuth(refreshTicket: false, cancellationToken);
            var header = ApiHelper.CreateHeader(
                token: token.token,
                appIdKey: _GfrApiAccessInfo.ApiAppId,
                contentType: string.Empty,
                ApiHelper.TokenTypeBasic
            );

            var url = $"{ApiURL(_gfrEndPointsOptions.RouteWorkFlow)}";
            var request = new RouteWorkflowRequest(
                FilingId: new List<string> { firmFlowId },
                CurrentStep: new List<string> {},
                Complete: true,
                NextStep: WorkFlowRouteConstants.NextStep,
                Priority: WorkFlowRouteConstants.Priority,
                Status: WorkFlowRouteConstants.Status,
                RoutingNote: WorkFlowRouteConstants.RoutingNote,
                AssignedDate: DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt"),
                CompletedDate: DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt"),
                AssignedTo: WorkFlowRouteConstants.AssignedTo,
                EmailNotify: false);

            var jsonRequestBody = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, ApiHelper.ContentTypeAppJson);
            var response = await _apiHelper.ExecutePostAsync(url, jsonRequestBody, header, cancellationToken);      
            if(response.HasFailure)
            {
                _logger.LogError("Error updating route for firmFlowID : {FirmFlow}}", firmFlowId);
                return new BatchExtensionException($"Error updating route for firmFlowID : {firmFlowId}"); 
            }

            var final = await _apiHelper.DeserializeResult<RouteWorkflowResponse>(response.Value);
            if (final.HasFailure)
            {
                _logger.LogError("Error getting Work Flow {Failure}", final.Failure);
                return new BatchExtensionException($"Error updating route for firmFlowID : {firmFlowId}"); 
            }

            return Possible.Completed;
    




        } catch (Exception ex)
        {            
            _logger.LogError(ex, "Unable to update Work Flow {firmFlowId}", firmFlowId);  
            return new BatchExtensionException($"Unable to update Work Flow {firmFlowId}", ex);
        }   
        

    }

    public async Task<Possible<BatchExtensionException>> UpdateFirmFlowDueDate(
        List<BatchExtensionDeliverableData> deliverableData, 
        IBatchService batchService,
        BatchExtensionData document,        
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating due date for firmflow Id: {ID} in UpdateFirmFlowDueDate", document.FirmFlowId);
        
        var errors = new List<string>();
        var processed = new List<string>();

        var tracking  = await GetTrackingReportByDeliverables(document.ClientNumber, cancellationToken);
        if(tracking.HasFailure)
        {
             return new BatchExtensionException($"Error in UpdateFirmFlowDueDate: {tracking.Failure}");  
        }

        var header = ApiHelper.CreateHeader(
            token: token.token,
            appIdKey: _GfrApiAccessInfo.ApiAppId,
            contentType: string.Empty,
            ApiHelper.TokenTypeBasic
        );
        var url = $"{ApiURL(_gfrEndPointsOptions.EditWorkFlow)}";

        var trackingProcess = tracking.Value.FirmFlowReportResponse
            .Where(x =>             
                _processOptions.AllowedGfrWorkFlows.Contains(x.Workflow ?? "")
            )
            .ToList();
            
        foreach (var item in trackingProcess)
        {
        
            var extendedDataResponse = GetDeliverableExtensionDate(deliverableData, item.Deliverables);
            var requestBody = new EditWorkflowRequest(
                int.Parse(item.FilingID),           
                Deliverable: new EditWorkFlowDeliverable(
                    WorkFlowRouteConstants.Action, 
                    extendedDataResponse.ExtensionDate.ToString("MM/dd/yyyy"), 
                    extendedDataResponse.Deliverable
                )
            ); 

            StringContent content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, ApiHelper.ContentTypeAppJson);    
            
            var createResponse = await _apiHelper.ExecutePostAsync(url, content, header, cancellationToken);
            if (createResponse.HasFailure)
            {
                errors.Add($"Error in UpdateFirmFlowDueDate Getting response for {item.FilingID} : {createResponse.Failure}");
                await batchService.UpdateBatchItemDueDateExtendedFailed(document.BatchItemGuid);
                continue;                     
            }
            var final = await _apiHelper.DeserializeResult<EditWorkFlowResponse>(createResponse.Value);
            if (final.HasFailure)
            {
                errors.Add($"Error in UpdateFirmFlowDueDate  DeserializeResult for {item.FilingID} : {createResponse.Failure}");
                await batchService.UpdateBatchItemDueDateExtendedFailed(document.BatchItemGuid);
                continue;                       
            }         

            await batchService.UpdateBatchItemDueDateExtendedSuccessfull(document.BatchItemGuid);
            processed.Add($"{item.FilingID}");
        }
        
        if(errors.Any())
        {
            _logger.LogError(
                "Batch extension failed with {ErrorCount} errors. Errors: {Errors}",
                errors.Count,
                errors);                        
        }

        if (processed.Any())
        {
            return Possible.Completed;
        }

        return new BatchExtensionException($"Updating due dates for FirmFlowId: {errors}");
        
    }
    
    private (DateTime ExtensionDate, string Deliverable)  GetDeliverableExtensionDate(List<BatchExtensionDeliverableData> deliverableData, string Deliverable)
    {
        
        var deliverableDataFiltered = deliverableData.FirstOrDefault(_ => Deliverable.Equals(_.Deliverable));
        
        if (deliverableDataFiltered is null)  
        {
            return (DateTime.MinValue, "Deliverable not found");
        }

         return (deliverableDataFiltered.ExtensionDate, deliverableDataFiltered.Deliverable);

    }

    private async Task<Either<TrackingReportByDeliverableResponse, BatchExtensionException>> GetTrackingReportByDeliverables(string clientNumnber, CancellationToken cancellationToken = default)
    {
        
        //if(tracking.IsEmptyDeliverable)
        //{
            _logger.LogInformation("In GetTrackingReportByWorkflow");
            try
            {
                await BuildClientAuth(refreshTicket: false, cancellationToken);     
                
                var header = ApiHelper.CreateHeader(
                    token: token.token,
                    appIdKey: _GfrApiAccessInfo.ApiAppId,
                    contentType: string.Empty,
                    ApiHelper.TokenTypeBasic
                );
            
                List<IndexItem> indexItem = new()
                {
                    new(WorkFlowRouteConstants.ClientNumberIndex, clientNumnber),
                    new(WorkFlowRouteConstants.TaxYearIndex, DateTime.Now.AddYears(-1).Year.ToString())
                };
                
                var requestBody = new TrackingReportByDeliverableRequest(     
                    _GfrApiAccessInfo.DrawerId ,"CORE TAX","","false", indexItem);

                var jsonRequestBody = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, ApiHelper.ContentTypeAppJson);  

                var url = ApiURL(_gfrEndPointsOptions.TrackingReportByDeliverable);
                var response = await _apiHelper.ExecutePostAsync(url, jsonRequestBody, header, cancellationToken);
                if (response.HasFailure)
                {
                    _logger.LogError(response.Failure,"Error getting TrackingReportByWorkflow {Failure}", response.Failure);                   
                    return new BatchExtensionException($"Error in GetTrackingReportByWorkflow: {response.Failure.Message}");
                }                
                var final = await _apiHelper.DeserializeResult<TrackingReportByDeliverableResponse>(response.Value);
                if (final.HasFailure)
                {
                    _logger.LogError(response.Failure,"Error getting TrackingReportByWorkflow {Failure}", response.Failure);                   
                    return new BatchExtensionException($"Unable to get Tracking Report: {final.Failure.Message}");
                }                
                return final.Value;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message ,"Error getting TrackingReportByWorkflow {Failure}", ex.Message);               
                return new BatchExtensionException($"Unable to get Tracking Report: {ex.Message}");
            }
        //}
    }

    private async Task<Possible<BatchExtensionException>> UploadDocumentAsync(
            string documentId,
            byte[] documentData,
            string fileName,
            CancellationToken cancellationToken)
    {

        _logger.LogInformation("Uploading document: {ID} in UploadDocumentAsync", documentId);
        
        var header = ApiHelper.CreateHeader(
            token: token.token,
            appIdKey: _GfrApiAccessInfo.ApiAppId,
            contentType: string.Empty,
            ApiHelper.TokenTypeBasic
        );

        var url = $"{ApiURL(_gfrEndPointsOptions.UploadDocument).Replace("{documentID}", documentId)}";
        using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(documentData);
                form.Add(fileContent, "file", fileName);

        int retryAttempts = 0;
        while (retryAttempts < _processOptions.GfrUploadRetryLimit)
        {
             var response = await _apiHelper.ExecutePostAsync(url, form, header, cancellationToken);
            if (response.HasFailure)
            {
                if (retryAttempts >= _processOptions.GfrUploadRetryLimit)
                {
                    _logger.LogError(response.Failure, "Error in UploadDocumentAsync: {ErrorMessage}", response.Failure.Message);
                    return new BatchExtensionException($"Error in UploadDocumentAsync for documentId:{documentId}:{response.Failure.Message}");
                } 
                retryAttempts++;               
            } else
            {
                _logger.LogInformation("Successfully uploaded document: {DocumentId}", documentId);
                return Possible.Completed;
            }
                
        }
        return new BatchExtensionException($"Error in UploadDocumentAsync for documentId:{documentId} after {_processOptions.GfrUploadRetryLimit} retries.");       
    }

    private async Task<Either<List<GetIndexResponse>, BatchExtensionException>> GetIndexesByDrawerIdAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In GetIndexesByDrawerIdAsync");
        try
        {
            await BuildClientAuth(refreshTicket: false, cancellationToken);     
            
            var header = ApiHelper.CreateHeader(
                token: token.token,
                appIdKey: _GfrApiAccessInfo.ApiAppId,
                contentType: string.Empty,
                ApiHelper.TokenTypeBasic
            );

            var url = ApiURL(_gfrEndPointsOptions.GetIndexes).Replace("{drawerID}", _GfrApiAccessInfo.DrawerId);
            var response = await _apiHelper.ExecuteGetAsync(url, header, cancellationToken);
            if (response.HasFailure)
            {
                _logger.LogError(response.Failure, "Unable to get Indexes for drawer {ErrorMessage}", response.Failure.Message);
                return new BatchExtensionException($"Unable to get Indexes for drawer {_GfrApiAccessInfo.DrawerId} : {response.Failure.Message}");
            }
            var final = await _apiHelper.DeserializeResult<List<GetIndexResponse>>(response.Value);
            if (final.HasFailure)
            {
                _logger.LogError(response.Failure, "Unable to get Indexes for drawer {ErrorMessage}", response.Failure.Message);
                return new BatchExtensionException($"Unable to get Indexes for drawer {_GfrApiAccessInfo.DrawerId} : {final.Failure.Message}");
            }            
           
            return final.Value;

        }
        catch (Exception ex)
        {

            return new BatchExtensionException($"Unable to get Indexes for drawer {_GfrApiAccessInfo.DrawerId} : {ex.Message}");
        }


    }


    private async Task<Either<List<GfrClient>, BatchExtensionException>> GetClientsByDrawerAsync(        
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("In GetClientsByDrawerAsync");

        try
        {
            await BuildClientAuth(refreshTicket: false, cancellationToken); 
            var header = ApiHelper.CreateHeader(
                token: token.token,
                appIdKey: _GfrApiAccessInfo.ApiAppId,
                contentType: string.Empty,
                ApiHelper.TokenTypeBasic
            );

            var url = ApiURL(_gfrEndPointsOptions.GetClientsByDrawerId).Replace("{drawerId}", _GfrApiAccessInfo.DrawerId);          
            var response = await _apiHelper.ExecuteGetAsync(url, header, cancellationToken);
            if( response.HasFailure)
            {
                _logger.LogError(response.Failure, "Unable to get Clients for drawer {DrawerId} {Message}", _GfrApiAccessInfo.DrawerId, response.Failure.Message);
                return new BatchExtensionException($"Unable to get Clients for drawer {_GfrApiAccessInfo.DrawerId} : {response.Failure.Message}"); 
            }

            var final = await _apiHelper.DeserializeResult<List<GfrClient>>(response.Value);
            if (final.HasFailure)
            {
                _logger.LogError(response.Failure, "Unable to Deserialize Clients in GetClientsByDrawerAsync for drawer {DrawerId} {Message}", _GfrApiAccessInfo.DrawerId, final.Failure.Message);
                return new BatchExtensionException($"Unable to Deserialize Clients in GetClientsByDrawerAsync for drawer {_GfrApiAccessInfo.DrawerId} : {final.Failure.Message}");
            }                       
            return final.Value;

        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unable to get Clients for drawer {DrawerId} {Message}", _GfrApiAccessInfo.DrawerId, ex.Message);  
            return new BatchExtensionException($"Unable to get Clients for drawer {_GfrApiAccessInfo.DrawerId} : {ex.Message}");
        }
    }

    private async Task<Either<string, BatchExtensionException>> CreateDocumentAsync(
          List<GetIndexResponse> indexes,          
          GfrDocument document,
          CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In {Proc}", "CreateDocumentAsync");
        _logger.LogDebug("Creating GFR document: {ID} for client: {Number}...", "", "");
        var url = ApiURL(_gfrEndPointsOptions.CreateDocument);
        var requestBody = new CreateDocumentRequest(           
            _GfrApiAccessInfo.DrawerId, 
            document,       
            indexes
        );
        var jsonRequestBody = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, ApiHelper.ContentTypeAppJson);        
        var header = ApiHelper.CreateHeader(
            token: token.token,
            appIdKey: _GfrApiAccessInfo.ApiAppId,
            contentType: string.Empty,
            ApiHelper.TokenTypeBasic
        );
        var response = await _apiHelper.ExecutePostAsync(url, jsonRequestBody, header, cancellationToken);
        if (response.HasFailure)
        {
            return new BatchExtensionException($"Unable to create GFR document : {response.Failure.Message}");
        }
        var final = await _apiHelper.DeserializeResult<CreateDocumentResponse>(response.Value);
        if (final.HasFailure)
        {
            return new BatchExtensionException($"Unable to create GFR document");
        }

        if (final.Value.IndexValidation.Errors.Count > 0)
        {
            _logger.LogError(@"Error getting Documment ID {Errors}", System.Text.Json.JsonSerializer.Serialize(final.Value.IndexValidation.Errors.ToList()));
        }

        return final.Value.DocumentCreate.DocumentId;
    }


    

    private string ApiURL(string api) => String.Concat(_gfrEndPointsOptions.Domain, "/", api);


}
