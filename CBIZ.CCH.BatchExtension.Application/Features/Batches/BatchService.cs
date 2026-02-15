using Microsoft.Extensions.Logging;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using Microsoft.Extensions.Options;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;
using System.Runtime.InteropServices;


namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public class BatchService(
        IBatchRepository batchRepository,
        IOptions<ProcessOptions> processOptions,
        ILogger<BatchService> logger) : IBatchService
{
    private readonly IBatchRepository _batchRepository = batchRepository;
    private readonly ILogger<BatchService> _logger = logger;
    private readonly ProcessOptions _processOptions = processOptions.Value;

    List<BatchExtensionDeliverableData> tracking = new List<BatchExtensionDeliverableData>();

    #region BatchExtensionData 
    public Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFR(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusGFR(batchItemGuid, BatchExtensionDataItemStatus.GfrUploadGood, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFRFailed(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusGFR(batchItemGuid, BatchExtensionDataItemStatus.GfrUploadError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCH(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchDownloadGood, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCHFailed(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchDownloadError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemUpdateGfrDocumentId(Guid batchItemGuid, string gfrDocumentId, CancellationToken cancellationToken = default)
        => UpdateBatchItemGfrDocumentId(batchItemGuid, BatchExtensionDataItemStatus.GfrDocumentCreated, gfrDocumentId, cancellationToken);
    
    public Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchCreated(Guid batchGuid,CancellationToken cancellationToken = default)
        => UpdateBatchStatus(batchGuid, BatchExtensionDataItemStatus.CchBatchCreated , string.Empty, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchFailed(Guid batchGuid, string message, CancellationToken cancellationToken = default)
        => UpdateBatchStatus(batchGuid, BatchExtensionDataItemStatus.CchBatchCreatedError, message, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemCCHStatusFailed(Guid batchItemGuid ,CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchBatchCreatedError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDueDateExtendedSuccessfull(Guid batchItemGuid ,CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrDueDateExtendedGood, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDueDateExtendedFailed(Guid batchItemGuid ,CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrDueDateExtendedError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchStatusFailed(Guid batchId, string message, CancellationToken cancellationToken = default)
        => UpdateBatchStatus(batchId, BatchExtensionDataItemStatus.StatusBad, message, cancellationToken);

    public async Task<Either<List<BatchExtensionDataWithReturnType>,BatchExtensionException>> GetBatchExtensionData(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _batchRepository.GetBatchExtensionDataByDaysAsync(cancellationToken);
            return request;

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchExtensionData");
            return new BatchExtensionException($"Error Getting BatchExtensionData for 30 days ", ex);
        }
    }
    
    public Task<Possible<BatchExtensionException>> UpdateBatchItemsCreateBatchFailed(Guid queueId, string message, CancellationToken cancellationToken = default)
        => UpdateBatchItemsCreateBatchFailed(queueId, BatchExtensionDataItemStatus.CchBatchCreatedError, message, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemStatusDateExtended(Guid batchItemGuid, CancellationToken cancellationToken = default )
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrDueDateExtendedGood, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateBatchItemStatusDateExtendedFailed(Guid batchItemGuid, CancellationToken cancellationToken = default )
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrDueDateExtendedError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRoute(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrWorkFlowRouteGood, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteFailed(Guid batchItemGuid, string message, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrWorkFlowRouteError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteUpdated(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrWorkFlowRouteGood, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteUpdatedFailed(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrWorkFlowRouteError, cancellationToken);
    
    private async Task<Possible<BatchExtensionException>> UpdateBatchItemStatusCCH(Guid batchItemGuid, BatchExtensionDataItemStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var rep = await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
                s => s                    
                    .SetProperty(b => b.FileDownLoadedFromCCH, b => status.Code == BatchExtensionDataItemStatus.CchDownloadGood.Code)
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateBatchItemDownloadedFromCCH");
            return new BatchExtensionException($"Error adding to the database in UpdateBatchItemDownloadedFromCCH:batchItemGuid{batchItemGuid} ", ex);
        }
    }

    private async Task<Possible<BatchExtensionException>> UpdateBatchStatus(Guid batchId, 
        BatchExtensionDataItemStatus status, 
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {

            await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchId == batchId,
                  s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.Message, b => message)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                    cancellationToken);
            return Possible.Completed;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateBatchStatus");
            return new BatchExtensionException($"Error adding to the database in UpdateBatchStatus: batchGuid{batchId} ", ex);
        }



    }

    private async Task<Possible<BatchExtensionException>> UpdateBatchItemStatusGFR(Guid batchItemGuid, BatchExtensionDataItemStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var rep = await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
                s => s
                    .SetProperty(b => b.FileUploadedToGFR, b => status.Code == BatchExtensionDataItemStatus.GfrUploadGood.Code)
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateBatchItemDownloadedFromCCH");
            return new BatchExtensionException($"Error adding to the database in UpdateBatchItemDownloadedFromCCH:batchItemGuid{batchItemGuid} ", ex);
        }
    }
    
    private async Task<Possible<BatchExtensionException>> UpdateBatchItemGfrDocumentId(Guid batchItemGuid, BatchExtensionDataItemStatus status, string gfrDocumentId, CancellationToken cancellationToken = default)
    {      
        try
        {
            var rep = await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
                s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.GfrDocumentId, b => gfrDocumentId)                                   
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateBatchItemDownloadedFromCCH");
            return new BatchExtensionException($"Error adding to the database in UpdateBatchItemDownloadedFromCCH:batchItemGuid{batchItemGuid} ", ex);
        }
    }
    
    private async Task<Possible<BatchExtensionException>> UpdateBatchItemStatus(Guid batchItemGuid, BatchExtensionDataItemStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var rep = await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
                s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateBatchItemStatus");
            return new BatchExtensionException($"Error adding to the database in UpdateBatchItemStatus:batchItemGuid{batchItemGuid} ", ex);
        }
    }
    
    private async Task<Possible<BatchExtensionException>> UpdateBatchItemsCreateBatchFailed(Guid queueIdGuid, BatchExtensionDataItemStatus status, string message, CancellationToken cancellationToken = default)
    {
        
        try
        {
            var rep = await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.QueueIDGUID == queueIdGuid,
                s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.Message, b => message)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateBatchItemStatus");
            return new BatchExtensionException($"Error adding to the database in UpdateBatchItemsStatus:queueIdGuid{queueIdGuid} ", ex);
        }
    }
    
    #endregion


    #region BatchExtensionQueue 
    public async Task<Either<BatchExtensionQueue, BatchExtensionException>> GetRequest(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _batchRepository.GetBatchQueueById(queueId, cancellationToken);
            return request;

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetRequest");
            return new BatchExtensionException($"Error Getting request fror: {queueId} ", ex);
        }
    }
    
    public async Task<Either<Guid, BatchExtensionException>> AddToQueue(BatchExtensionQueue queue, CancellationToken cancellationToken = default)
    {

        try
        {
            var rep = await _batchRepository.AddToBatchQueue(queue, cancellationToken);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in AddToQueue");
            return new BatchExtensionException($"Error adding to the database in AddToQueue: {queue} ", ex);
        }

    }

    public async Task<Either<List<BatchQueueStatusResponse>, BatchExtensionException>> GetQueueStatus(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _batchRepository.GetQueueStatus(queueId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetQueueStatus for {QueueId}", queueId);
            return new BatchExtensionException($"{queueId}", ex);
        }
    }
    
    public Task<Possible<BatchExtensionException>> UpdateQueueStatusToComplete(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateQueueStatus(queueId, BatchQueueStatus.Completed, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateQueueStatusToCompleteWithErrors(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateQueueStatus(queueId, BatchQueueStatus.CompletedWithErrors, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateQueueStatusToScheduled(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateQueueStatus(queueId, BatchQueueStatus.Scheduled, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateQueueStatusToRunning(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateQueueStatus(queueId, BatchQueueStatus.Running, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusCCHBatchFail(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateQueueBatchStatus(queueId, BatchExtensionDataItemStatus.CchBatchCreatedError, cancellationToken);

    public  Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusRanSuccessfull(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateQueueBatchStatus(queueId, BatchExtensionDataItemStatus.StatusGood, cancellationToken);

     public  Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusRanError(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateQueueBatchStatus(queueId, BatchExtensionDataItemStatus.StatusBad, cancellationToken);

    private async Task<Possible<BatchExtensionException>> UpdateQueueStatus(Guid queueId, string status, CancellationToken cancellationToken = default)
    {
        try
        {

            await _batchRepository.UpdateBatchExtensionQueueAsync<BatchExtensionQueue>(
                 x => x.QueueId == queueId,
                   s => s
                     .SetProperty(b => b.QueueStatus, b => status),
                     cancellationToken);
            return Possible.Completed;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateQueueStatus for {QueueId}", queueId);
            return new BatchExtensionException($"{queueId}", ex);
        }
    }

   private async Task<Possible<BatchExtensionException>> UpdateQueueBatchStatus(Guid queueId, BatchExtensionDataItemStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            await _batchRepository.UpdateBatchExtensionQueueAsync<BatchExtensionQueue>(
                 x => x.QueueId == queueId,
                   s => s
                     .SetProperty(b => b.BatchStatus, b => status.Description),
                     cancellationToken);
            return Possible.Completed;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateQueueStatus for {QueueId}", queueId);
            return new BatchExtensionException($"{queueId}", ex);
        }
    }
    
    #endregion
    
    #region BatchExtensionDeliverableData
    public async Task<Either<List<BatchExtensionDeliverableData>, BatchExtensionException>> GetExtensionDeliverableAsync(CancellationToken cancellationToken = default)
    {

        if(tracking.Count > 0) return tracking;
        try
        {
            var ret = await _batchRepository.GetExtensionDeliverableAsync(cancellationToken);
            if (ret.HasFailure)
            {
                _logger.LogError(ret.Failure, "Error occurred in GetExtensionDeliverableAsync :{Message}", ret.Failure.Message);
                return new BatchExtensionException($"Error occurred in GetExtensionDeliverableAsync : {ret.Failure.Message}");
            }
            tracking = ret.Value;
            return tracking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetExtensionDeliverableAsync");
            return new BatchExtensionException($"Error occurred in GetExtensionDeliverableAsync", ex);
        }
    }
    #endregion
    
}
