using Microsoft.Extensions.Logging;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;


namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public class BatchService(
        IBatchRepository batchRepository,       
        ILogger<BatchService> logger) : IBatchService
{
    private readonly IBatchRepository _batchRepository = batchRepository;
    private readonly ILogger<BatchService> _logger = logger;
    
    List<BatchExtensionDeliverableData> tracking = new List<BatchExtensionDeliverableData>();

    #region BatchExtensionData 
    public Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFR(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusGFR(batchItemGuid, BatchExtensionDataItemStatus.GfrUploadGood, string.Empty, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFRFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusGFR(batchItemGuid, BatchExtensionDataItemStatus.GfrUploadError, exception, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCH(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchDownloadGood, null, string.Empty, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCHFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchDownloadError, exception.Message, exception.ToString(), cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemUpdateGfrDocumentId(Guid batchItemGuid, string gfrDocumentId, CancellationToken cancellationToken = default)
        => UpdateBatchItemGfrDocumentId(batchItemGuid, BatchExtensionDataItemStatus.GfrDocumentCreated, gfrDocumentId, cancellationToken);
    
    public Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchCreated(Guid batchGuid,CancellationToken cancellationToken = default)
        => UpdateBatchStatus(batchGuid, BatchExtensionDataItemStatus.CchBatchCreated, string.Empty, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchFailed(Guid batchGuid, BatchExtensionException exception, CancellationToken cancellationToken = default)
        => UpdateBatchStatus(batchGuid, BatchExtensionDataItemStatus.CchBatchCreatedError, exception, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemCCHStatusFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusCCH(batchItemGuid, BatchExtensionDataItemStatus.CchBatchCreatedError, exception.Message, exception.ToString(), cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDueDateExtendedSuccessfull(Guid batchItemGuid ,CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrDueDateExtendedGood, null, string.Empty, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemDueDateExtendedFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrDueDateExtendedError, exception.Message, exception.ToString(), cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchStatusFailed(Guid batchId,  string message , CancellationToken cancellationToken = default)
        => UpdateBatchStatus(batchId, BatchExtensionDataItemStatus.StatusBad, message, cancellationToken);
    
    public async Task<Either<List<BatchExtensionData>, BatchExtensionException>> GetBatchExtensionDataByQueueId(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _batchRepository.GetBatchExtensionDataByQueueId(queueId, cancellationToken);
            return request;

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchExtensionDataByQueueId");
            return new BatchExtensionException($"Error Getting GetBatchExtensionDataByQueueId ", ex);
        }
    }
    public async Task<Either<List<BatchExtensionDataWithReturnType>,BatchExtensionException>> GetBatchExtensionData(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _batchRepository.GetBatchExtensionDataByDaysAsync(cancellationToken);
            return request;

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchExtensionData");
            return new BatchExtensionException($"Error Getting BatchExtensionData", ex);
        }
    }
    
    public async Task<Either<PagedResult<BatchExtensionData>, BatchExtensionException>> GetBatchExtensionDataPaged(
        int pageNumber,
        int pageSize,
        string[]? filterField,
        string[]? filterValue,
        string? sortField,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _batchRepository.GetBatchExtensionDataPagedAsync(
                pageNumber,
                pageSize,
                filterField,
                filterValue,
                sortField,
                sortDescending,
                cancellationToken);

            if (result.HasFailure)
                return result.Failure;
          
            var convertedItems = result.Value.Data.Select(item => new BatchExtensionData
            {
                Id = item.Id,
                QueueIDGUID = item.QueueIDGUID,
                FirmFlowId = item.FirmFlowId,
                TaxReturnId = item.TaxReturnId,
                ClientName = item.ClientName,
                ClientNumber = item.ClientNumber,
                OfficeLocation = item.OfficeLocation,
                EngagementType = item.EngagementType,
                BatchId = item.BatchId,
                BatchItemGuid = item.BatchItemGuid,
                BatchItemStatus = item.BatchItemStatus,
                StatusDescription = item.StatusDescription,
                FileName = item.FileName,
                FileDownLoadedFromCCH = item.FileDownLoadedFromCCH,
                FileUploadedToGFR = item.FileUploadedToGFR,
                GfrDocumentId = item.GfrDocumentId,
                Message = item.Message,
                CreationDate = item.CreationDate,
                UpdatedDate = item.UpdatedDate
            }).ToList();

            var pagedResult = new PagedResult<BatchExtensionData>(
                convertedItems,
                result.Value.PageNumber,
                result.Value.PageSize,
                result.Value.TotalRecords,
                result.Value.TotalPages);

            return pagedResult;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchExtensionDataPaged");
            return new BatchExtensionException($"Error Getting BatchExtensionDataPaged", ex);
        }
    }

     public Task<Possible<BatchExtensionException>> UpdateBatchItemsCreateBatchFailed(Guid queueId, BatchExtensionException exception, CancellationToken cancellationToken = default)        
        => UpdateBatchItemStatus(queueId, BatchExtensionDataItemStatus.CchBatchCreatedError, exception.Message, exception.ToString(), cancellationToken);


    public Task<Possible<BatchExtensionException>> UpdateBatchItemStatusDateExtended(Guid batchItemGuid, CancellationToken cancellationToken = default )
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrDueDateExtendedGood, string.Empty, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateBatchItemStatusDateExtendedFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default )
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrDueDateExtendedError, exception.Message, exception.ToString(), cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRoute(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrWorkFlowRouteGood, string.Empty, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrWorkFlowRouteError, exception.Message, exception.ToString(), cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteUpdated(Guid batchItemGuid, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrWorkFlowRouteGood, string.Empty, cancellationToken);
    public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteUpdatedFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatus(batchItemGuid, BatchExtensionDataItemStatus.GfrWorkFlowRouteError, exception.Message, exception.ToString(), cancellationToken);
    

     public Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteFailedByBatchExtensionId(Guid batchExtensionId, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusByBatchExtensionId(batchExtensionId, BatchExtensionDataItemStatus.GfrWorkFlowRouteError, cancellationToken);

    public Task<Possible<BatchExtensionException>> UpdateBatchItemsRequeued(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateBatchItemStatusByQueueId(queueId, BatchExtensionDataItemStatus.Requeue, cancellationToken);

    private Task<Possible<BatchExtensionException>> UpdateBatchItemStatusCCH(
            Guid batchItemGuid, 
            BatchExtensionDataItemStatus status, 
            string? message = null,
            string? rawMessage = null, 
            CancellationToken cancellationToken = default)
        {
            
            return ExecuteUpdate(() =>
                _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                    x => x.BatchItemGuid == batchItemGuid,
                    s => s
                        .SetProperty(b => b.FileDownLoadedFromCCH, b => status.Code == BatchExtensionDataItemStatus.CchDownloadGood.Code)
                        .SetProperty(b => b.BatchItemStatus, b => status.Code)
                        .SetProperty(b => b.StatusDescription, b => status.Description)
                        .SetProperty(b => b.Message, b => message)
                        .SetProperty(b => b.RawMessage, b => rawMessage)
                        .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                    cancellationToken),
                $"Error adding to the database in UpdateBatchItemStatus: batchItemGuid {batchItemGuid}"
            );
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


    private async Task<Possible<BatchExtensionException>> UpdateBatchStatus(Guid batchId, 
        BatchExtensionDataItemStatus status, 
        BatchExtensionException exception,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = exception.Message;
            var rawMessage = exception.ToString(); 

            await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchId == batchId,
                  s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.Message, b => message) 
                    .SetProperty(b => b.RawMessage, b => rawMessage)       
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


    private Task<Possible<BatchExtensionException>> UpdateBatchItemStatusGFR(
        Guid batchItemGuid, 
        BatchExtensionDataItemStatus status, 
        string message, 
        CancellationToken cancellationToken = default)
    {
        
        return ExecuteUpdate(() =>
            _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
                s => s
                    .SetProperty(b => b.FileUploadedToGFR, b => status.Code == BatchExtensionDataItemStatus.GfrUploadGood.Code)
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.Message, b => message)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                    cancellationToken),
            $"Error adding to the database in UpdateBatchItemStatusGFR:batchItemGuid{batchItemGuid}"
        );
    }
    
    private async Task<Possible<BatchExtensionException>> UpdateBatchItemStatusGFR(Guid batchItemGuid, BatchExtensionDataItemStatus status, BatchExtensionException exception, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = exception.Message;
            var rawMessage = exception.ToString(); 

            var rep = await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
                s => s
                    .SetProperty(b => b.FileUploadedToGFR, b => status.Code == BatchExtensionDataItemStatus.GfrUploadGood.Code)
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.Message, b => message)
                    .SetProperty(b => b.RawMessage, b => rawMessage)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateBatchItemStatusGFR");
            return new BatchExtensionException($"Error adding to the database in UpdateBatchItemStatusGFR:batchItemGuid{batchItemGuid} ", ex);
        }
    }




    private Task<Possible<BatchExtensionException>> UpdateBatchItemGfrDocumentId(
        Guid batchItemGuid, 
        BatchExtensionDataItemStatus status, 
        string gfrDocumentId, 
        CancellationToken cancellationToken = default)
    {      
        return ExecuteUpdate(() =>
            _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
               x => x.BatchItemGuid == batchItemGuid,
                s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.GfrDocumentId, b => gfrDocumentId)                                   
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken),
           $"Error adding to the database in UpdateBatchItemGfrDocumentId:batchItemGuid{batchItemGuid} "
        );              
    }
    
    private async Task<Possible<BatchExtensionException>> UpdateBatchItemStatus(Guid batchItemGuid, BatchExtensionDataItemStatus status, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var rep = await _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
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
            return new BatchExtensionException($"Error adding to the database in UpdateBatchItemStatus:batchItemGuid{batchItemGuid} ", ex);
        }
    }

    private Task<Possible<BatchExtensionException>> UpdateBatchItemStatus(
        Guid batchItemGuid,
        BatchExtensionDataItemStatus status,
        string? message = null,
        string? rawMessage = null,
        CancellationToken cancellationToken = default)
    {
        
        return ExecuteUpdate(() =>
            _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.BatchItemGuid == batchItemGuid,
                s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.Message, b => message)
                    .SetProperty(b => b.RawMessage, b => rawMessage)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken),
            $"Error adding to the database in UpdateBatchItemStatus: batchItemGuid {batchItemGuid}"
        );
    }



    private Task<Possible<BatchExtensionException>> UpdateBatchItemStatusByBatchExtensionId(
        Guid batchExtensionId, 
        BatchExtensionDataItemStatus status, 
        CancellationToken cancellationToken = default)
    {
        return ExecuteUpdate(() =>
            _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.Id == batchExtensionId,
                s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken),
            $"Error adding to the database in UpdateBatchItemStatus:batchExtensionId {batchExtensionId}"
        );       
    }
    
    private Task<Possible<BatchExtensionException>> UpdateBatchItemStatusByQueueId(Guid queueIdGuid, BatchExtensionDataItemStatus status, CancellationToken cancellationToken = default)    
    {

        return ExecuteUpdate(() =>
            _batchRepository.UpdateBatchExtensionItemAsync<BatchExtensionData>(
                x => x.QueueIDGUID == queueIdGuid,
                s => s
                    .SetProperty(b => b.BatchItemStatus, b => status.Code)
                    .SetProperty(b => b.StatusDescription, b => status.Description)
                    .SetProperty(b => b.UpdatedDate, b => DateTime.Now),
                cancellationToken),
            $"Error adding to the database in UpdateBatchItemStatusByQueueId:QueueId {queueIdGuid}"
        );
    }
    
    #endregion


    #region BatchExtensionQueue 

    public async Task<Either<LaunchBatchRunRequest, BatchExtensionException>> GetLaunchBatchQueueRequestByQueueId(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _batchRepository.GetLaunchBatchRequestByqueueId(queueId, cancellationToken);
            return request;

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetRequest");
            return new BatchExtensionException($"Error Getting request fror: {queueId} ", ex);
        }
    }
    
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

    public async Task<Either<List<Guid>, BatchExtensionException>> GetScheduledBatchQueueIds(CancellationToken cancellationToken = default)
    {
         try
        {                              
            var rep = await _batchRepository.GetScheduledBatchQueueIds(cancellationToken);
            return rep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetScheduledBatchQueueIds");
            return new BatchExtensionException($"Error getting Guids from the database in GetScheduledBatchQueueIds ", ex);
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
    
    public Task<Possible<BatchExtensionException>> UpdateQueueStatusToRequeued(Guid queueId, CancellationToken cancellationToken = default)
        => UpdateQueueStatus(queueId, BatchQueueStatus.ReQueued, cancellationToken);



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
            var batchStatus = status switch
            {
              BatchQueueStatus.Running => BatchQueueStatus.Running,
              BatchQueueStatus.ReQueued => BatchQueueStatus.ReQueued,
              _ => null
            };

            await _batchRepository.UpdateBatchExtensionQueueAsync<BatchExtensionQueue>(
                 x => x.QueueId == queueId,
                   s => s
                     .SetProperty(b => b.QueueStatus, b => status)
                     .SetProperty(b => b.BatchStatus, b => batchStatus ?? b.BatchStatus),
                     cancellationToken);
            _logger.LogInformation("QueueId {QueueID} status updated to {Status}", queueId, status);
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
    public async Task<Either<List<BatchExtensionDeliverableData>, BatchExtensionException>> GetExtensionDeliverableAsync(
        string queueReturnType, 
        CancellationToken cancellationToken = default)
    {
        
        try
        {
            var ret = await _batchRepository.GetExtensionDeliverableAsync(queueReturnType, cancellationToken);
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
    
    private async Task<Possible<BatchExtensionException>> ExecuteUpdate(
        Func<Task<Possible<BatchExtensionException>>> action,
        string errorMessage)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage}", errorMessage);
            return new BatchExtensionException(errorMessage, ex);
        }
    }
}
