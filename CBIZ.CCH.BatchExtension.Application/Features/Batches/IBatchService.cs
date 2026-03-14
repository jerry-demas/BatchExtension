using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public interface IBatchService
{
    Task<Either<BatchExtensionQueue,BatchExtensionException>> GetRequest(Guid queueId, CancellationToken cancellationToken = default);
    Task<Either<Guid, BatchExtensionException>> AddToQueue(BatchExtensionQueue queue, CancellationToken cancellationToken = default);
    Task<Either<List<BatchQueueStatusResponse>, BatchExtensionException>> GetQueueStatus(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToRequeued(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToScheduled(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToRunning(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToComplete(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToCompleteWithErrors(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusCCHBatchFail(Guid queueId, CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> UpdateBatchItemCCHStatusFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusRanSuccessfull(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusRanError(Guid queueId, CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> UpdateBatchItemDueDateExtendedSuccessfull(Guid batchItemGuid ,CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemDueDateExtendedFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default);


    Task<Either<LaunchBatchRunRequest, BatchExtensionException>> GetLaunchBatchQueueRequestByQueueId(Guid queueId, CancellationToken cancellationToken = default);

    Task<Either<List<Guid>, BatchExtensionException>> GetScheduledBatchQueueIds(CancellationToken cancellationToken = default);

    Task<Either<List<BatchExtensionDataWithReturnType>,BatchExtensionException>> GetBatchExtensionData(CancellationToken cancellationToken = default);

    Task<Either<List<BatchExtensionData>, BatchExtensionException>> GetBatchExtensionDataByQueueId(Guid queueId, CancellationToken cancellationToken = default);

    Task<Either<List<BatchExtensionDeliverableData>,BatchExtensionException>> GetExtensionDeliverableAsync(string queueReturnType,CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchCreated(Guid batchGuid, CancellationToken cancellationToken = default);   
    Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchFailed(Guid batchGuid, BatchExtensionException exception, CancellationToken cancellationToken = default);    
    Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCH(Guid batchItemGuid, CancellationToken cancellationToken = default);
    
    Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCHFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFR(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemUpdateGfrDocumentId(Guid batchItemGuid, string gfrDocumentId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFRFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchStatusFailed(Guid batchId, string message, CancellationToken cancellationToken = default);    
    Task<Possible<BatchExtensionException>> UpdateBatchItemsCreateBatchFailed(Guid queueId, BatchExtensionException exception, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemsRequeued(Guid queueId, CancellationToken cancellationToken = default);
    
    Task<Possible<BatchExtensionException>> UpdateBatchItemStatusDateExtended(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemStatusDateExtendedFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default);


    Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRoute(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default);
    
    
    Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteUpdated(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteUpdatedFailed(Guid batchItemGuid, BatchExtensionException exception, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemWorkFlowRouteFailedByBatchExtensionId(Guid batchExtensionId, CancellationToken cancellationToken = default);   
    
    Task<Either<PagedResult<BatchExtensionData>, BatchExtensionException>> GetBatchExtensionDataPaged(
        int pageNumber,
        int pageSize,
        string[]? filterField,
        string[]? filterValue,
        string? sortField,
        bool sortDescending,
        CancellationToken cancellationToken = default);
}
