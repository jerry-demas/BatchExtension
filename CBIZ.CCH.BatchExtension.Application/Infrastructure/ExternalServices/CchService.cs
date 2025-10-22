using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using CBIZ.CCH.BatchExtension.ApplicationFeatures.Batches;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;


using CBIZ.CCH.BatchExtension.Application.Shared;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;

public class CchService : ICchService
{
    private readonly ILogger<CchService> _logger;
    private readonly CCHEndPointOptions _cchEndPointsOptions;
    private readonly ProcessOptions _processOptions;
    //private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiHelper _apiHelper;

    public CchService(
        ILogger<CchService> logger,
        IOptions<CCHEndPointOptions> cchEndPointsOptions,
        IOptions<ProcessOptions> processOptions,
        //IHttpClientFactory httpClientFactory,
        ApiHelper apiHelper

        )
    {
        _logger = logger;
        _cchEndPointsOptions = cchEndPointsOptions.Value;
        _processOptions = processOptions.Value;
        //_httpClientFactory = httpClientFactory;
        _apiHelper = apiHelper;
    }
    public async Task<Either<string, BatchExtensionException>> CreateBatchAsync(
        string returnType,
        List<string> returnIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine(ApiURL(_cchEndPointsOptions.CreateBatchAPI));

            var url = $"{ApiURL(_cchEndPointsOptions.CreateBatchAPI)}/{returnType}";
            StringContent content = new StringContent(JsonSerializer.Serialize(returnIds), Encoding.UTF8, "application/json");
            BatchExtensionException? exception = null;
            string executionId = string.Empty;

            var createResponse = await _apiHelper.ExecutePostAsync(url, content, null, cancellationToken);           
            createResponse.Match(
                async success =>
                {
                    var jsonContent = await success.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<CreateBatchResult>(
                            jsonContent,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                    if (result is null)
                    {
                        exception = new BatchExtensionException($"Create batch CreateBatchAsync for returns: {returnIds}");
                        return;
                    }
                    executionId = result.ExecutionId.ToString();
                    
                },
                failure => {
                    exception = new BatchExtensionException(
                            $"Error in CreateBatchAsync: {failure.Message}");
                        }
            );
            
            if (!executionId.Equals(string.Empty))
                return executionId;

            if (exception is not null)
                return exception;

            return new BatchExtensionException($"Unknown error creating batch for returnIds: {string.Join(", ", returnIds)}");   
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Error creating batch for returnIds: {returnIds}", string.Join(", ", returnIds));
            return new BatchExtensionException($"Error creating batch for returnIds: {returnIds}", ex);
        }


        //step 2
        //https://developers.cchaxcess.com/api-details#api=oip-tax-service&operation=post-returns-export-batch
        //https://api.cchaxcess.com/taxservices/oiptax/api/v1/ReturnsExportBatch
        //BatchGuid eq 'dc0a94ef-318b-488d-bc2e-b898a8a8d3d5'
        //gets SubItemExecutionIDS dc0a94ef-318b-488d-bc2e-b898a8a8d3d5
        /*
            {
            "ExecutionID": "6d4454f5-0e02-4740-9462-2684e4ef854b",
            "FileResults": [{
                "FileGroupID": 1,
                "IsError": false,
                "Messages": ["dc0a94ef-318b-488d-bc2e-b898a8a8d3d5 submitted successfully."],
                "SubItemExecutionIDs": ["dc0a94ef-318b-488d-bc2e-b898a8a8d3d5"]
            }]
            }
        */
    }
   
    public async Task<Either<string, BatchExtensionException>> GetBatchStatusAsync(
        Guid executionId,
        int returnsCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(BatchStatusTimeDelay(returnsCount), cancellationToken);
            var url = $"{ApiURL(_cchEndPointsOptions.GetBatchStatusAPI)}/{executionId}";
            int retryAttempts = 0;

            while (retryAttempts < _processOptions.StatusRetryLimit)
            {
                var statusResponse = await _apiHelper.ExecuteGetAsync(url);

                string? batchStatus = null;
                BatchExtensionException? exception = null;

                statusResponse.Match(
                    async success =>
                    {

                        var jsonContent = await success.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<GetBatchStatusResponse>(
                                jsonContent,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                        if (result is null)
                        {
                            exception = new BatchExtensionException(
                                $"Create batch file status null for ExecutionId: {executionId}");
                            return;
                        }
                        if (result.BatchStatusDescription == BatchRecordStatus.Complete.Description ||
                            result.BatchStatusDescription == BatchRecordStatus.Exception.Description)
                        {
                            batchStatus = result.BatchStatus;
                            return;
                        }                                           
                    },
                    failure =>
                    {
                        exception = new BatchExtensionException(
                            $"Error in ExecuteGetAsync: {failure.Message}");
                    });

                if (batchStatus is not null)
                    return batchStatus;

                if (exception is not null)
                    return exception;

                retryAttempts++;
                
                await Task.Delay(BatchStatusTimeDelay(returnsCount), cancellationToken);
            }

            return new BatchExtensionException($"Batch did not complete after {_processOptions.StatusRetryLimit} retries.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for Execution ID: {executionId}", executionId);
            return new BatchExtensionException($"Error getting status for Execution ID: {executionId}");
        }
         //step 3
        //https://developers.cchaxcess.com/api-details#api=oip-tax-service&operation=get-batch-status
        //https://api.cchaxcess.com/taxservices/oiptax/api/v1/BatchStatus[?$filter]
        // if complete
    }


    public async Task<Either<List<CreateBatchOutputFilesResponse>, BatchExtensionException>> CreateBatchOutputFilesAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {

        try
        {
            Console.WriteLine(ApiURL(_cchEndPointsOptions.GetBatchOutputFilesAPI));
            var url = $"{ApiURL(_cchEndPointsOptions.GetBatchOutputFilesAPI)}/{executionId}";

            BatchExtensionException? exception = null;
            List<CreateBatchOutputFilesResponse>? result = null;

            var response = await _apiHelper.ExecuteGetAsync(url, null, cancellationToken);
            response.Match(
               async success =>
               {
                    var jsonContent = await success.Content.ReadAsStringAsync();
                    result = JsonSerializer.Deserialize<List<CreateBatchOutputFilesResponse>>(
                       jsonContent,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       });

                    if (result is null)
                    {
                        exception = new BatchExtensionException($"Create batch CreateBatchOutputFilesAsync for ID: {executionId}");
                        return;
                    }
                                       
               },
               failure =>
               {
                   exception = new BatchExtensionException($"Error downloading file from API: {failure.Message}");
               }
           );
            
            if (result is not null)
                return result;

            if (exception is not null)
                return exception;
            
            return new BatchExtensionException($"Unexpected error: CreateBatchOutputFilesAsync for ID: {executionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch output files for Execution ID : {executionId}", executionId);
            return new BatchExtensionException($"Error creating batch output files for Execution ID : {executionId}");
        }
    }

    public async Task<Possible<BatchExtensionException>> DownloadBatchOutputFilesAsync(
        Guid executionId,
        Guid batchGUID,
        string fileName,
        CancellationToken cancellationToken = default)
    {

        try
        {
            Console.WriteLine(ApiURL(_cchEndPointsOptions.BatchOutputDownloadFileAPI));
            var url = $"{ApiURL(_cchEndPointsOptions.BatchOutputDownloadFileAPI)}/{executionId}/{batchGUID}/{fileName}";
            BatchExtensionException? exception = null;           
            var response = await _apiHelper.ExecuteGetAsync(url);
            response.Match(
                async success =>
                {
                    HelperFunctions.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), _processOptions.DownloadFilesDirectory));
                    var returnFileName = Path.Combine(Directory.GetCurrentDirectory(), _processOptions.DownloadFilesDirectory, fileName);
                    await using (Stream contentStream = await success.Content.ReadAsStreamAsync(),
                        fileStream = new FileStream(returnFileName,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
                    {
                        await contentStream.CopyToAsync(fileStream, cancellationToken);
                    }                    

                },
                failure =>
                {
                    exception = new BatchExtensionException($"Error downloading file from API: {failure.Message}");                    
                }
            );

            //if (fileStreamReturn is not null)
            //    return fileStreamReturn;

            if (exception is not null)
                return exception;

            //return new BatchExtensionException($"Unexpected error: no response received for file {fileName}");
            return Possible.Completed;

                                   
        }
        catch (Exception ex)
        {
            
            return new BatchExtensionException($"Error downloading file from API: {ex}");
        };



        //step 4
        //BatchPOutput file
        //https://developers.cchaxcess.com/api-details#api=oip-tax-service&operation=get-batch-output-files
        //https://api.cchaxcess.com/taxservices/oiptax/api/v1/BatchOutputFiles[?$filter]
        /*
        * [{
        "BatchItemGuid": "7bbeaf88-a2a7-4741-9625-c9058cff67c8",
        "FileName": "output.zip or 2020US Test Acct V1.pdf"
        }]
        */


        //step5
        //https://developers.cchaxcess.com/api-details#api=oip-tax-service&operation=get-batch-output-download-files
        //https://api.cchaxcess.com/taxservices/oiptax/api/v1/BatchOutputDownloadFile[?$filter]

    }

    private string ApiURL(string cchAPI) => String.Concat(_cchEndPointsOptions.Domain, "/", cchAPI);
    private int BatchStatusTimeDelay(int returnCount = 1)
    {
        var milliSeconds = _processOptions.StatusTimeIntervalSeconds * 1000;
        return milliSeconds * returnCount;
    }
      
    
}
