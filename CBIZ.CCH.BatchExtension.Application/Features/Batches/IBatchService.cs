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
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToScheduled(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToRunning(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToComplete(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToCompleteWithErrors(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusCCHBatchFail(Guid queueId, CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> UpdateBatchItemCCHStatusFailed(Guid batchItemGuid, CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusRanSuccessfull(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueBatchStatusRanError(Guid queueId, CancellationToken cancellationToken = default);


    Task<Either<List<BatchExtensionData>,BatchExtensionException>> GetBatchExtensionData(CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchCreated(Guid batchGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemCreateCCHBatchFailed(Guid batchGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCH(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCHFailed(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFR(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemUpdateGfrDocumentId(Guid batchItemGuid, string gfrDocumentId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFRFailed(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchStatusFailed(Guid batchId, CancellationToken cancellationToken = default);

    
    


   
}
