
using System.Text.Json;
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Process;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CBIZ.CCH.BatchExtension.Presentation.BackgroundService;

public sealed class Worker : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _services;    
    private readonly ILogger<Worker> _logger;

    
    public Worker(
        IServiceProvider services,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger )
    {
        _services = services;       
        _logger = logger;      
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Worker starting at: {DateTimeOffset.Now}");
        
        _logger.LogInformation("Worker starting at: {DateTime}", DateTimeOffset.Now);

        using IServiceScope serviceScope = _services.CreateScope();

        var batchQueue = serviceScope.ServiceProvider.GetRequiredService<BatchQueue>();

        BatchProcessContext processContext = new BatchProcessContext(
            serviceScope.ServiceProvider.GetRequiredService<IBatchRepository>(),
            serviceScope.ServiceProvider.GetRequiredService<IBatchService>(),
            serviceScope.ServiceProvider.GetRequiredService<ICchService>(),
            serviceScope.ServiceProvider.GetRequiredService<IGfrService>(),
            serviceScope.ServiceProvider.GetRequiredService<IEmailService>()
        );

        
        while (!stoppingToken.IsCancellationRequested)
        {           
            try
            {
                await ProcessQueueAsync(batchQueue, processContext, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker encountered an unhandled exception.");                
                await processContext.emailService.SendEmailFailureAsync("MyEmailAddress", $"Process failed: {ex.Message}", stoppingToken);
            }
        }
        
        Console.WriteLine($"Worker stopping at: {DateTimeOffset.Now}");        
    }


    private async Task ProcessQueueAsync(
        BatchQueue batchQueue,
        BatchProcessContext processContext,
        CancellationToken stoppingToken)
    {


        var reader = batchQueue.Reader;

        while (await reader.WaitToReadAsync(stoppingToken))
        {
            _logger.LogInformation("Queue reader heartbeat");

            var queueItems = new List<BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>>();
         
            while (reader.TryRead(out var queue))
                queueItems.Add(queue);

            if (queueItems.Count == 0)
                continue;
            
            foreach (var queueItem in queueItems)
            {
                bool isFailed = false;

                try
                {
                    _logger.LogInformation("Processeing QueueId : {QueueId}", queueItem.Request.QueueId);

                    await HandleQueueItemAsync(queueItem, processContext, stoppingToken);
                
                    _logger.LogInformation("Stopped processing QueueId : {QueueId}", queueItem.Request.QueueId);

                    isFailed = queueItem.Tcs.Task.IsFaulted;

                } catch (Exception ex)
                {
                    isFailed = true;
                    _logger.LogError(
                        ex,
                        "Queue processing crashed for {QueueId}",
                        queueItem.Request.QueueId);
                }

                try
                {
                    if (isFailed)
                    {
                        await processContext.batchService.UpdateQueueStatusToCompleteWithErrors(
                            queueItem.Request.QueueId, stoppingToken);
                    }
                    else
                    {
                        await processContext.batchService.UpdateQueueStatusToComplete(
                            queueItem.Request.QueueId, stoppingToken);
                    }

                    var getStatusItems =
                        await processContext.batchService.GetQueueStatus(
                            queueItem.Request.QueueId, stoppingToken);

                    var queueRequest =
                        await processContext.batchService.GetRequest(
                            queueItem.Request.QueueId, stoppingToken);

                    if (isFailed)
                    {
                        await processContext.emailService.SendEmailFailedQueueProcessAsync(
                            queueRequest.Value.SubmittedBy,
                            getStatusItems.Value,
                            stoppingToken);
                    }
                    else
                    {
                        await processContext.emailService.SendEmailSuccessfullQueueProcessAsync(
                            queueRequest.Value.SubmittedBy,
                            getStatusItems.Value,
                            stoppingToken);
                    }

                    }  catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Post-processing failed for {QueueId}",
                        queueItem.Request.QueueId);
                }               
            }
        }

    }

private async Task HandleQueueItemAsync(
    BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse> queueItem,
    BatchProcessContext processContext,
    CancellationToken token)
{
    Guid queueId = queueItem.Request.QueueId;
    try
    {
        
        var requestResult = await GetBatchQueue(processContext.batchRepository, queueId);
        if (requestResult.HasFailure)
        {
            await FailQueueAsync(queueItem, processContext, queueId, $"Updating error status for queue {queueId}", token);
            return;
        }

        var request = requestResult.Value;

        var batchResponse = await processContext.batchService.GetBatchExtensionDataByQueueId(queueId, token);
        if (batchResponse.HasFailure || batchResponse.Value.Count == 0)
        {
           await FailQueueAsync(queueItem, processContext, queueId, $"Error retrieving batch extension data for queue {queueId}", token);
           return;            
        } 
        
        var batch = batchResponse.Value;

        if ((await UpdateQueueStatusToRunning(processContext.batchService, queueId)).HasFailure)
        {
            await FailQueueAsync(queueItem, processContext, queueId, $"Updating error status for queue {queueId}", token);
            return;
        }
       
        var createBatch = await CchCreateBatch(processContext, batch, request, queueId, token);
        if (createBatch.HasFailure)
        {            
            await processContext.batchService.UpdateBatchItemsCreateBatchFailed(queueId, createBatch.Failure, token);           
            await FailQueueAsync(queueItem, processContext, queueId, createBatch.Failure.Message, token);
            return;
        }
        var batchGuidId = createBatch.Value;

        await ProcessBatchStepsAsync(batch, batchGuidId, queueItem, processContext, token);
    }
    catch (Exception ex)
    {
        queueItem.Tcs.SetException(ex);
        await processContext.emailService.SendEmailFailureAsync("Myemailaddress" ,$"Process failed for BatchQueue:{queueId}: {ex.Message}", token);
        _logger.LogError(ex, "Error processing queue item {QueueId}", queueId);
    }
}

private async Task ProcessBatchStepsAsync(
    List<BatchExtensionData> batch,
    Guid batchGuidId,
    BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse> queueItem,
    BatchProcessContext processContext,
    CancellationToken token)
{
    var queueId = queueItem.Request.QueueId;
    if ((await UpdateBatchDataToDB(processContext.batchRepository, batch, batchGuidId, token)).HasFailure)
    {
        await FailQueueAsync(queueItem, processContext, queueId, "Error updating batch data", token);
        return;
    }
    await processContext.batchService.UpdateBatchItemCreateCCHBatchCreated(batchGuidId,token);

    var getBatchResponse  = await GetBatchStatus(processContext, batch, queueId, batchGuidId, batch.Count, token);
    if(getBatchResponse.HasFailure)
    {
        await FailQueueAsync(queueItem, processContext, queueId, getBatchResponse.Failure.Message ,token);
        return;
    }

    if ((await UpdateBatchDataToDB(processContext.batchRepository, batch, batchGuidId, token)).HasFailure)
    {
        await FailQueueAsync(queueItem, processContext, queueId, "Error updating batch data", token);
        return;
    }

    var createBatchFilesResponse = await CreateCCHBatchFiles(processContext, batch, queueId, batchGuidId, token);
    if(createBatchFilesResponse.HasFailure)
    {
        await FailQueueAsync(queueItem, processContext, queueId, createBatchFilesResponse.Failure.Message, token);        
        return;
    }

    if ((await UpdateBatchDataToDB(processContext.batchRepository, batch, batchGuidId, token)).HasFailure)
    {
        await FailQueueAsync(queueItem, processContext,queueId, "Error updating batch data", token);
        return;
    }

    var downloadUploadGfrResponse = await DownloadCCHFileAndUploadToGFR(processContext.cchService, processContext.batchService, processContext.gfrService, queueId, batch, token);
    if(downloadUploadGfrResponse.HasFailure)
    {
        await FailQueueAsync(queueItem, processContext, queueId, downloadUploadGfrResponse.Failure.Message, token);
        return;
    }
    
    await processContext.batchService.UpdateQueueBatchStatusRanSuccessfull(queueId, token); 
}

private async Task FailQueueAsync(
    BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse> queueItem,    
    BatchProcessContext processContext,       
    Guid queueId,
    string message,
    CancellationToken token = default)
    {
        LogErrorMessage(message);
        queueItem.Tcs.SetException(new BatchExtensionException(message));

        await processContext.batchService.UpdateQueueStatusToCompleteWithErrors(queueId, token);    
        await processContext.batchService.UpdateQueueBatchStatusRanError(queueId, token);                 
        await Task.CompletedTask;
    }
    

    private static async Task<Either<LaunchBatchRunRequest, BatchExtensionException>> GetBatchQueue(IBatchRepository repo, Guid queueId)
    {
       
        var batchQueueItem = await repo.GetBatchQueueById(queueId);
        if (batchQueueItem.HasFailure)
        {
            return new BatchExtensionException($"Error processing batch : {batchQueueItem.Failure.Message}");
        }

        LaunchBatchRunRequest? batchRequest = JsonSerializer.Deserialize<LaunchBatchRunRequest>(batchQueueItem.Value.QueueRequest);
        return batchRequest ?? new LaunchBatchRunRequest();
    }
    private static async Task<Possible<BatchExtensionException>> UpdateQueueStatusToRunning(IBatchService service, Guid queueId)
    {        
        var updateStatus = await service.UpdateQueueStatusToRunning(queueId);
        if (updateStatus.HasFailure)
        {           
            return new BatchExtensionException($"Updating error status for queue {queueId}");
        }

        return Possible.Completed; 
        
    }
    private static async Task<Either<Guid, BatchExtensionException>> CchCreateBatch(
        BatchProcessContext processContext,
        List<BatchExtensionData> batch,               
        LaunchBatchRunRequest request, 
        Guid queueId,
        CancellationToken stoppingToken)
    { 
        
        List<string> items = request.Returns.Select(r => r.ReturnId).Distinct().ToList();
        var executionIdResponse = await processContext.cchService.CreateBatchAsync(queueId, request.ReturnType, items, stoppingToken);
        if (executionIdResponse.HasFailure)
        {                                                   
            return executionIdResponse.Failure;
        }
        var newBatchId  = Guid.TryParse(executionIdResponse.Value, out var parsed) ? parsed : Guid.Empty;
        batch.UpdateBatchItemsBatchId(newBatchId);
        return newBatchId;
    }
    
    private static async Task<Possible<BatchExtensionException>> UpdateAllGfrFailedRoute(
        List<BatchExtensionData> batch,
        IGfrService gfrService,
        IBatchService batchService,
        CancellationToken stoppingToken )
    {      
        foreach (var flowId in batch.Select(_ => _.FirmFlowId))
        {
             await UpdateRouteFailed(flowId,gfrService,batchService,stoppingToken);
        }                
        return Possible.Completed;
    }


    private async Task<Either<string, BatchExtensionException>> GetBatchStatus(              
            BatchProcessContext processContext,
            List<BatchExtensionData> batch,
            Guid queueId,        
            Guid batchGuidId,
            int batchCount,           
            CancellationToken stoppingToken)
    {        
        _logger.LogInformation("GetBatchStatus for queue : {QueueId}", queueId);
        var statusCheckResult = await processContext.cchService.GetBatchStatusAsync(batchGuidId, batchCount, stoppingToken);
        if (statusCheckResult.HasFailure)
        {                                    
            await processContext.batchService.UpdateBatchItemCreateCCHBatchFailed(batchGuidId, statusCheckResult.Failure, stoppingToken);                  
            await processContext.batchService.UpdateQueueBatchStatusCCHBatchFail(queueId, stoppingToken);     
            await UpdateAllGfrFailedRoute(batch, processContext.gfrService, processContext.batchService, stoppingToken);
            return new BatchExtensionException($"Error getting status for batch {batchGuidId} : {statusCheckResult.Failure.Message}");
        }
        if (statusCheckResult.Value.status.Equals(BatchRecordStatus.Exception.Description))
        {
            await processContext.batchService.UpdateBatchItemCreateCCHBatchFailed(batchGuidId, new BatchExtensionException(ExtractErrorMessages(statusCheckResult.Value.items)), stoppingToken);            
            await processContext.batchService.UpdateQueueBatchStatusCCHBatchFail(queueId, stoppingToken);
            await UpdateAllGfrFailedRoute(batch, processContext.gfrService,processContext.batchService,stoppingToken);            
            return new BatchExtensionException($"Batch status returned {statusCheckResult.Value.status} {batchGuidId}");
        }

        batch.UpdateBatchItemsBatchGuidBatchStatus(statusCheckResult.Value.items);

        return statusCheckResult.Value.status;
        
    }

    private async Task<Either<List<BatchExtensionData>, BatchExtensionException>> CreateCCHBatchFiles(        
        BatchProcessContext processContext,
        List<BatchExtensionData> batch,
        Guid queueId,
        Guid batchGuidId, 
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("CreateCCHBatchFiles for queue : {QueueId}", queueId);
        var batchCreatOutputFilesResult = await processContext.cchService.CreateBatchOutputFilesAsync(batchGuidId, stoppingToken);
        if (batchCreatOutputFilesResult.HasFailure)
        {
            await UpdateAllGfrFailedRoute(batch, processContext.gfrService, processContext.batchService,stoppingToken);
            return new BatchExtensionException($"Error getting status for batch {batchGuidId} : {batchCreatOutputFilesResult.Failure.Message}");
        }        
        batch.UpdateBatchItemsFileNames(batchCreatOutputFilesResult.Value);
        return batch;
    }
    


    private static async Task<Possible<BatchExtensionException>> UpdateBatchDataToDB(
        IBatchRepository repo,
        List<BatchExtensionData> batch,
        Guid batchGuidId,
        CancellationToken stoppingToken)
    {
        var batchRepositoryResponse = await repo.UpdateBatchAsync(batch, batchGuidId, stoppingToken);
        if (batchRepositoryResponse.HasFailure)
            return new BatchExtensionException($"Error saving batch updates {batchGuidId} : {batchRepositoryResponse}");

        return Possible.Completed;    
    }

    private async Task<Possible<BatchExtensionException>> DownloadCCHFileAndUploadToGFR(
        ICchService cchService,
        IBatchService batchService,
        IGfrService gfrService,
        Guid queueid,
        List<BatchExtensionData> batch,
        CancellationToken stoppingToken)
    {
        var errors = new List<BatchExtensionException>();

        _logger.LogInformation(
            "DownloadCCHFileAndUploadToGFR for queue : {Queueid}", queueid);

        bool refreshGfrTicket = true;

        foreach (var item in batch)
        {
            if(item.BatchItemStatus == BatchExtensionDataItemStatus.CchBatchCreatedError.Code)
            {                
                errors.Add(new BatchExtensionException(item.Message));
                continue;
            }
            var result = await ProcessBatchItem(
                item,
                cchService,
                batchService,
                gfrService,
                refreshGfrTicket,
                stoppingToken);

            if (result.HasFailure)
                errors.Add(result.Failure);

            refreshGfrTicket = false;
        }

        if (errors.Count > 0)
        {
            var combined = string.Join(Environment.NewLine, errors.Select(e => e.Message));

            return new BatchExtensionException(
                $"Errors occurred while processing batch:{Environment.NewLine}{combined}");
        }

        return Possible.Completed;
}

    private async Task<Possible<BatchExtensionException>> ProcessBatchItem(
        BatchExtensionData item,
        ICchService cchService,
        IBatchService batchService,
        IGfrService gfrService,
        bool refreshGfrTicket,
        CancellationToken token)
        {
            var download = await DownloadBatchFileFromCCH(
                cchService,
                gfrService,
                batchService,
                item,
                token);

            if (download.HasFailure)
                return await HandleFailure(
                    item,                    
                    download.Failure,
                    batchService.UpdateBatchItemDownloadedFromCCHFailed,
                    batchService,
                    gfrService,
                    token);

            await batchService.UpdateBatchItemDownloadedFromCCH(item.BatchItemGuid, token);

            var upload = await UploadDocumentToGFR(
                item,
                batchService,
                gfrService,
                refreshGfrTicket,
                token);

            if (upload.HasFailure)
                return await HandleFailure(
                    item,                   
                    upload.Failure,
                    batchService.UpdateBatchItemUploadedToGFRFailed,
                    batchService,
                    gfrService,
                    token);

            await batchService.UpdateBatchItemUploadedToGFR(item.BatchItemGuid, token);

            var extend = await ExtendDueDate(item, gfrService, batchService, token);

            if (extend.HasFailure)
                return await HandleFailure(
                    item,
                    extend.Failure,
                    batchService.UpdateBatchItemDueDateExtendedFailed,
                    batchService,
                    gfrService,
                    token);

            await batchService.UpdateBatchItemDueDateExtendedSuccessfull(item.BatchItemGuid, token);

            var route = await UpdateRoute(
                item.QueueIDGUID,
                item.FirmFlowId,
                gfrService,
                batchService,
                token);

            if (route.HasFailure)
                return await HandleFailure(
                    item,
                    route.Failure,
                    batchService.UpdateBatchItemWorkFlowRouteFailed,
                    batchService,
                    gfrService,
                    token);

            await batchService.UpdateBatchItemWorkFlowRoute(item.BatchItemGuid, token);

            return Possible.Completed;
    }

    private static async Task<Possible<BatchExtensionException>> HandleFailure(
        BatchExtensionData item,        
        BatchExtensionException exception,       
        Func<Guid, BatchExtensionException, CancellationToken, Task> updateMethod,
        IBatchService batchService,
        IGfrService gfrService,
        CancellationToken token)
    {
        await updateMethod(item.BatchItemGuid, exception, token);

        await UpdateRouteFailed(
            item.FirmFlowId,
            gfrService,
            batchService,
            token);

        return new BatchExtensionException(
            $"{exception.Message} for BatchItemGuid {item.BatchItemGuid} : {item.FileName}");
    }


    private static async Task<Possible<BatchExtensionException>> DownloadBatchFileFromCCH(
        ICchService cccService,   
        IGfrService gfrService,    
        IBatchService batchService,
        BatchExtensionData extensionData,
        CancellationToken stoppingToken)
    {
       
        var downloadResult = await cccService.DownloadBatchOutputFilesAsync(
            extensionData.BatchId, 
            extensionData.BatchItemGuid, 
            extensionData.FileName, 
            stoppingToken);
        if (downloadResult.HasFailure){
            await gfrService.UpdateWorkFlowRouteFailed(extensionData.FirmFlowId, batchService, stoppingToken);            
            return new BatchExtensionException(downloadResult.Failure.Message);
        }

        return Possible.Completed;

    }
    
    private async Task<Possible<BatchExtensionException>> UploadDocumentToGFR(
           BatchExtensionData document,
           IBatchService batchService,
           IGfrService gfrService,
           bool refreshGfrTicket,
           CancellationToken stoppingToken)
    {
        _logger.LogInformation("UploadDocumentToGFR for queueId : {QueueIDGUID} BatchItemGuid : {BatchItemGuid}", document.QueueIDGUID, document.BatchItemGuid);
        var gfrUploadResult = await gfrService.UploadDocumentToGfr(
            new GfrDocument(
                document.TaxReturnId,
                document.FirmFlowId,
                document.FileName, 
                document.EngagementType,
                GfrObjectConstants.Document.ExtensionDocumentType, 
                document.ClientName, 
                document.ClientNumber,
                GfrObjectConstants.Document.ExtensionDocumentDescription),
            batchService,
            document,
            refreshGfrTicket,
            stoppingToken
            );
        if (gfrUploadResult.HasFailure)
        {           
            return new BatchExtensionException(gfrUploadResult.Failure.Message);
        }
        await batchService.UpdateBatchItemUploadedToGFR(document.BatchItemGuid, stoppingToken);
        
        return Possible.Completed;
    }

    private async Task<Possible<BatchExtensionException>> ExtendDueDate(
        BatchExtensionData document,
        IGfrService gfrService,
        IBatchService batchService,
        CancellationToken stoppingToken)
    {   
        _logger.LogInformation("ExtendDueDate for queueId : {QueueIDGUID} batchItemGUID : {BatchItemGuid}", document.QueueIDGUID, document.BatchItemGuid);
        var returnType = await batchService.GetQueueStatus(document.QueueIDGUID, stoppingToken) ;
        if(returnType.HasFailure)
        {
           return new BatchExtensionException(
                $"Error extending due date for FirmFlowId {document.FirmFlowId}. Cannot get Batch Extension deliverable data from database: {returnType.Failure.Message}"); 
        }

        var extensionDeliverableData = await batchService.GetExtensionDeliverableAsync(returnType.Value[0].ReturnType , stoppingToken);
        if(extensionDeliverableData.HasFailure)
        {
           return new BatchExtensionException(
                $"Error extending due date for FirmFlowId {document.FirmFlowId}. Cannot get Batch Extension deliverable data from database: {extensionDeliverableData.Failure.Message}"); 
        }

        var extendFilingIdDueDate = await gfrService.UpdateFirmFlowDueDate(
            extensionDeliverableData.Value,
            batchService,
            document, 
            stoppingToken);
        if (extendFilingIdDueDate.HasFailure)
        {
            return new BatchExtensionException($"Error extending due date for FirmFlowId {document.FirmFlowId} : {extendFilingIdDueDate.Failure.Message}");
        }                       
        return Possible.Completed;
    }

    private async Task<Possible<BatchExtensionException>> UpdateRoute(
        Guid queueId,
        string firmFlowID,        
        IGfrService gfrService,
        IBatchService batchService,               
        CancellationToken stoppingToken)
{       
        _logger.LogInformation("UpdateRoute for queueId: {QueueId} FirmFlowId: {FirmFlowId}", queueId, firmFlowID);
        var updateRoute = await gfrService.UpdateWorkFlowRouteProcessing(firmFlowID, batchService, stoppingToken);
        if(updateRoute.HasFailure)
        {            
            return updateRoute.Failure;
        }   
                             
        return Possible.Completed;
    }

    private static async Task<Possible<BatchExtensionException>> UpdateRouteFailed(
        string firmFlowID,       
        IGfrService gfrService,
        IBatchService batchService,               
        CancellationToken stoppingToken)
{       
        var updateRoute = await gfrService.UpdateWorkFlowRouteFailed(firmFlowID, batchService, stoppingToken);
        if(updateRoute.HasFailure)
        {
            return updateRoute.Failure;            
        }                                  
        return Possible.Completed;
    }


    private static string ExtractErrorMessages(List<BatchItemStatus> items)
    {
        if (items is null)
        {
            return string.Empty;
        }
        
        return string.Join(", ",
            items
                .Where(i => !string.IsNullOrWhiteSpace(i.ResponseDescription))
                .Select(i => i.ResponseDescription.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
        );

    }

    private void LogErrorMessage(string message) => _logger.LogError("{Message}", message);
}

