using Microsoft.Extensions.Logging;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using System.Security.Cryptography.X509Certificates;


namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public class BatchService(
        IBatchRepository batchRepository,
        ILogger<BatchService> logger) : IBatchService
{
    private readonly IBatchRepository _batchRepository = batchRepository;
    private readonly ILogger<BatchService> _logger = logger;


    #region BatchExtensionData 
    public Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFR(Guid batchExtensionDataId, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusGFR(batchExtensionDataId, BatchExtensionDataItemStatus.GfrUploadGood, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFRFailed(Guid batchExtensionDataId, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusGFR(batchExtensionDataId, BatchExtensionDataItemStatus.GfrUploadError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCH(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchDownloadGood, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCHFailed(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchDownloadError, cancellationToken);



    private async Task<Possible<BatchExtensionException>> UpdateBatchItemStatusCCH(Guid batchItemGuid, BatchExtensionDataItemStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var rep = await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
                s => s
                    .SetProperty(b => b.FileDownLoadedFromCCH, b => status.Code == BatchExtensionDataItemStatus.CchDownloadGood.Code ? true : false)
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

    private async Task<Possible<BatchExtensionException>> UpdateBatchStatus(Guid batchId, BatchExtensionDataItemStatus status, CancellationToken cancellationToken = default)
    {
        try
        {

            await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchId == batchId,
                  s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
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
                    .SetProperty(b => b.FileUploadedToGFR, b => status.Code == BatchExtensionDataItemStatus.GfrUploadGood.Code ? true : false)
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
    
    #endregion


    #region BatchExtensionQueue 

    public async Task<Either<Guid, BatchExtensionException>> AddToQueue(BatchExtensionQueue queue, CancellationToken cancellationToken = default)
    {

        try
        {
            var rep = await _batchRepository.AddToBatchQueue(queue);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in AddToQueue");
            return new BatchExtensionException($"Error adding to the database in AddToQueue: {queue} ", ex);
        }

    }

    public async Task<Either<BatchQueueStatusResponse, BatchExtensionException>> GetQueueStatus(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _batchRepository.GetQueueStatus(queueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred in GetQueueStatus for {queueId}");
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

    public Task<Possible<BatchExtensionException>> UpdateBatchStatusFailed(Guid batchId, CancellationToken cancellationToken = default)
        => UpdateBatchStatus(batchId, BatchExtensionDataItemStatus.StatusBad, cancellationToken);
    
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
            _logger.LogError(ex, $"Error occurred in UpdateQueueStatus for {queueId}");
            return new BatchExtensionException($"{queueId}", ex);
        }
    }

   
    
    #endregion
}
