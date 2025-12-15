using Microsoft.Extensions.Logging;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;


namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public class BatchService(
        IBatchRepository batchRepository,
        IOptions<ProcessOptions> processOptions,
        ILogger<BatchService> logger) : IBatchService
{
    private readonly IBatchRepository _batchRepository = batchRepository;
    private readonly ILogger<BatchService> _logger = logger;
    private readonly ProcessOptions _processOptions = processOptions.Value;

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
        => UpdateBatchStatus(batchGuid, BatchExtensionDataItemStatus.CchBatchCreated , cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchFailed(Guid batchGuid,CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchGuid, BatchExtensionDataItemStatus.CchBatchCreatedError , cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemCCHStatusFailed(Guid batchItemGuid ,CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchBatchCreatedError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchStatusFailed(Guid batchId, CancellationToken cancellationToken = default)
        => UpdateBatchStatus(batchId, BatchExtensionDataItemStatus.StatusBad, cancellationToken);

    public async Task<Either<List<BatchExtensionData>,BatchExtensionException>> GetBatchExtensionData(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _batchRepository.GetBatchExtensionDataByDaysAsync(_processOptions.ExtensionDataTimeSpanDays, cancellationToken);
            return request;

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchExtensionData");
            return new BatchExtensionException($"Error Getting BatchExtensionData for 30 days ", ex);
        }
    }



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
}
