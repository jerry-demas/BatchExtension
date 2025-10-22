using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public interface IBatchService
{
    Task<Either<Guid, BatchExtensionException>> AddToQueue(BatchExtensionQueue queue, CancellationToken cancellationToken = default);
    Task<Either<BatchQueueStatusResponse, BatchExtensionException>> GetQueueStatus(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToScheduled(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToRunning(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToComplete(Guid queueId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateQueueStatusToCompleteWithErrors(Guid queueId, CancellationToken cancellationToken = default);


    Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCH(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemDownloadedFromCCHFailed(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFR(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchItemUploadedToGFRFailed(Guid batchItemGuid, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateBatchStatusFailed(Guid batchId, CancellationToken cancellationToken = default);

    
    


   
}
