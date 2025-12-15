
using System.Security.Principal;
using System.Text.Json;
using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Process;
using CBIZ.CCH.BatchExtension.Application.Infrastructure;
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
            //LaunchBatchRunRequest request = await processContext.batchService.GetRequest(, stoppingToken))
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
        bool IsFailed = false;
        var reader = batchQueue.Reader;

        if (!await reader.WaitToReadAsync(stoppingToken))
            return;

        var queueItems = new List<BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>>();
        while (reader.TryRead(out var queue))
            queueItems.Add(queue);

        var queueItem = queueItems.FirstOrDefault();
        if (queueItem == null) return;

        await HandleQueueItemAsync(queueItem, processContext, stoppingToken);

        IsFailed = queueItems.Any(q => q.Tcs.Task.IsFaulted);
        if(IsFailed) 
        {
            await processContext.batchService.UpdateQueueStatusToCompleteWithErrors(queueItem.Request.QueueId,stoppingToken);
        } else 
        {
            await processContext.batchService.UpdateQueueStatusToComplete(queueItem.Request.QueueId,stoppingToken);
        }
            
        var getStatusItems = await processContext.batchService.GetQueueStatus(queueItem.Request.QueueId, stoppingToken);
        var queueRequest = await processContext.batchService.GetRequest(queueItem.Request.QueueId, stoppingToken);
        if(IsFailed) 
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
            await processContext.emailService.SendEmailFailedQueueProcessAsync(queueRequest.Value.SubmittedBy, getStatusItems.Value, stoppingToken);
        } else 
        {        
            await processContext.emailService.SendEmailSuccessfullQueueProcessAsync(queueRequest.Value.SubmittedBy, getStatusItems.Value, stoppingToken);
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
            await FailQueueAsync(queueItem, processContext.batchService, queueId, $"Updating error status for queue {queueId}", token);
            return;
        }

        var request = requestResult.Value;

        if ((await UpdateQueueStatusToRunning(processContext.batchService, queueId)).HasFailure)
        {
            await FailQueueAsync(queueItem, processContext.batchService, queueId, $"Updating error status for queue {queueId}", token);
            return;
        }

        var createBatch = await CchCreateBatch(processContext.cchService, request, token);
        if (createBatch.HasFailure)
        {
            await FailQueueAsync(queueItem, processContext.batchService, queueId, createBatch.Failure.Message, token);
            return;
        }
        var batchGuidId = createBatch.Value;
        var batch = request.ConvertToBatchExtensionData(queueId, batchGuidId).ToList();

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

    if ((await AddBatchToDB(processContext.batchRepository, batch, token)).HasFailure)
    {
        await FailQueueAsync(queueItem, processContext.batchService, queueId, "Error adding batches to database", token);
        return;
    }
    
    if ((await GetBatchStatus(processContext.cchService, processContext.batchService, queueId, batchGuidId, batch.Count, token)).HasFailure)
    {
        await FailQueueAsync(queueItem, processContext.batchService, queueId, "Error getting batch status" ,token);
        return;
    }

    if ((await CreateCCHBatchFiles(processContext.cchService, batch, batchGuidId, token)).HasFailure)
    {        
        await FailQueueAsync(queueItem, processContext.batchService, queueId,"Error creating batch files", token);        
        return;
    }
    
    if ((await UpdateBatchDataToDB(processContext.batchRepository, batch, batchGuidId, token)).HasFailure)
    {
        await FailQueueAsync(queueItem, processContext.batchService, queueId, "Error updating batch data", token);
        return;
    }

    if ((await DownloadCCHFileAndUploadToGFR(processContext.cchService, processContext.batchService, processContext.gfrService, batch, token)).HasFailure)
    {       
        await FailQueueAsync(queueItem, processContext.batchService, queueId, "Error uploading batch file", token);                 
    } else
    {
        await processContext.batchService.UpdateQueueBatchStatusRanSuccessfull(queueId, token); 
    }
         
}

private async Task FailQueueAsync(
    BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse> queueItem,
    IBatchService batchService,
    Guid queueId,
    string message,
    CancellationToken token = default)
    {
        LogErrorMessage(message);
        queueItem.Tcs.SetException(new BatchExtensionException(message));

        await batchService.UpdateQueueStatusToCompleteWithErrors(queueId, token);    
        await batchService.UpdateQueueBatchStatusRanError(queueId, token);   
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
        ICchService service,        
        LaunchBatchRunRequest request, 
        CancellationToken stoppingToken)
    { 
        List<string> items = request.Returns.Select(r => r.ReturnId).Distinct().ToList();
        var executionIdResponse = await service.CreateBatchAsync(request.ReturnType, items, stoppingToken);
        if (executionIdResponse.HasFailure)
        {                           
            return new BatchExtensionException($"Error processing batch : {executionIdResponse.Failure.Message}");
        }
        return Guid.TryParse(executionIdResponse.Value, out var parsed) ? parsed : Guid.Empty;
    }


    private static async Task<Possible<BatchExtensionException>> AddBatchToDB(IBatchRepository repo, List<BatchExtensionData> batch, CancellationToken stoppingToken)
    {
        var batchResult = await repo.AddBatchAsync(batch, stoppingToken);
        if (batchResult.HasFailure)
        {
            return new BatchExtensionException($"Error adding batches to the database : {batchResult.Failure.Message}");
        }
        return Possible.Completed;
    }

    private static async Task<Either<string, BatchExtensionException>> GetBatchStatus(
            ICchService service,   
            IBatchService batchService, 
            Guid queueId,        
            Guid batchGuidId,
            int batchCount,
            CancellationToken stoppingToken)
    {
        
        var statusCheckResult = await service.GetBatchStatusAsync(batchGuidId, batchCount, stoppingToken);
        if (statusCheckResult.HasFailure)
        {
            await batchService.UpdateBatchItemCreateCCHBatchFailed(batchGuidId, stoppingToken);
            await batchService.UpdateQueueBatchStatusCCHBatchFail(queueId, stoppingToken);
            return new BatchExtensionException($"Error getting status for batch {batchGuidId} : {statusCheckResult.Failure.Message}");
        }
        if (statusCheckResult.Value.status.Equals(BatchRecordStatus.Exception.Description))
        {
            await batchService.UpdateBatchItemCreateCCHBatchFailed(batchGuidId, stoppingToken);
            await batchService.UpdateQueueBatchStatusCCHBatchFail(queueId, stoppingToken);
            return new BatchExtensionException($"Batch status returned {statusCheckResult.Value.status} {batchGuidId}");
        }
        //If complete, check to see if multiple items
        if(statusCheckResult.Value.status.Equals(BatchRecordStatus.Complete.Description))
        {
            var itemsException = statusCheckResult.Value.items.Where(r => r.ItemStatusCode == BatchItemRecordStatus.Exception.Code);                        
            foreach (var item in itemsException)
            {   
                await batchService.UpdateBatchItemCCHStatusFailed(item.ItemGuid, stoppingToken);
            }
        }

        return statusCheckResult.Value.status;
        
    }

    private static async Task<Either<List<BatchExtensionData>, BatchExtensionException>> CreateCCHBatchFiles(
        ICchService service,
        List<BatchExtensionData> batch,
        Guid batchGuidId, CancellationToken stoppingToken)
    {
        var batchCreatOutputFilesResult = await service.CreateBatchOutputFilesAsync(batchGuidId, stoppingToken);
        if (batchCreatOutputFilesResult.HasFailure)
        {
            return new BatchExtensionException($"Error getting status for batch {batchGuidId} : {batchCreatOutputFilesResult.Failure.Message}");
        }        
        batch.UpdateBatchItemsGuidAndFileName(batchCreatOutputFilesResult.Value);
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

    private static async Task<Possible<BatchExtensionException>> DownloadCCHFileAndUploadToGFR(
        ICchService cchService,
        IBatchService batchService,
        IGfrService gfrService,
        List<BatchExtensionData> batch,
        CancellationToken stoppingToken)
    {
        var errors = new List<BatchExtensionException>();
        
        bool refreshGfrTicket = true;

        foreach (var batchExtensionDataItem in batch)
        {            
            var downloadResult = await DownloadBatchFileFromCCH(
                cchService,               
                batchExtensionDataItem.BatchId,
                batchExtensionDataItem.BatchItemGuid,
                batchExtensionDataItem.FileName,
                stoppingToken);

            if (downloadResult.HasFailure)
            {
                errors.Add(new BatchExtensionException($"Download failed for BatchItemGuid {batchExtensionDataItem.BatchItemGuid} : {batchExtensionDataItem.FileName}"));
                await batchService.UpdateBatchItemDownloadedFromCCHFailed(batchExtensionDataItem.BatchItemGuid, stoppingToken);
            }
            else
            {
                await batchService.UpdateBatchItemDownloadedFromCCH(batchExtensionDataItem.BatchItemGuid, stoppingToken);
                var uploadResult = await UploadDocumentToGFR(batchExtensionDataItem, batchService, gfrService, refreshGfrTicket, stoppingToken);
                if (uploadResult.HasFailure)
                {
                    errors.Add(new BatchExtensionException($"upload to GFR failed for BatchItemGuid {batchExtensionDataItem.BatchItemGuid} : {batchExtensionDataItem.FileName}"));
                    await batchService.UpdateBatchItemUploadedToGFRFailed(batchExtensionDataItem.BatchItemGuid, stoppingToken);
                }
                else
                {
                    await batchService.UpdateBatchItemUploadedToGFR(batchExtensionDataItem.BatchItemGuid, stoppingToken);                                       
                }
                refreshGfrTicket = false;
            }
        }

        if (errors.Count > 0)
        {            
            var combined = string.Join(Environment.NewLine, errors.Select(e => e.Message));            
            return new BatchExtensionException($"Errors occurred while processing batch:{Environment.NewLine}{combined}");
        }
        
        return Possible.Completed;
    }

    private static async Task<Possible<BatchExtensionException>> DownloadBatchFileFromCCH(
        ICchService cccService,
        Guid batchId,
        Guid batchItemGuid,
        string fileName,
        CancellationToken stoppingToken)
    {

        var downloadResult = await cccService.DownloadBatchOutputFilesAsync(batchId, batchItemGuid, fileName, stoppingToken);
        if (downloadResult.HasFailure)
            return new BatchExtensionException($"Error downloading document {batchItemGuid} : {fileName}");

        return Possible.Completed;

    }
    
    private static async Task<Possible<BatchExtensionException>> UploadDocumentToGFR(
           BatchExtensionData document,
           IBatchService batchService,
           IGfrService gfrService,
           bool refreshGfrTicket,
           CancellationToken stoppingToken)
    {
        var gfrUploadResult = await gfrService.UploadDocumentToGfr(
            new GfrDocument(document.TaxReturnId, document.FirmFlowId, document.FileName, document.EngagementType, GfrObjectConstants.Document.ExtensionDocumentType , document.ClientName, document.ClientNumber),
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

    private void LogErrorMessage(string message) => _logger.LogError("{Message}", message);
}

