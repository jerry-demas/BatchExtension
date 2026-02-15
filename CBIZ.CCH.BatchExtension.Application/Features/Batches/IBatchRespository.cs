
using System.Linq.Expressions;

using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

using Microsoft.EntityFrameworkCore.Query;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public interface IBatchRepository
{


  Task<Either<Guid, BatchExtensionException>> AddToBatchQueue(BatchExtensionQueue batchQueue, CancellationToken cancellationToken = default);
  Task<Either<BatchExtensionQueue, BatchExtensionException>> GetBatchQueueById(Guid queueItemId, CancellationToken cancellationToken = default);
  Task<Either<List<BatchQueueStatusResponse>, BatchExtensionException>> GetQueueStatus(Guid queueId, CancellationToken cancellationToken = default);

  Task<Either<List<BatchExtensionData>, BatchExtensionException>> GetBatchAsync(Guid batchExtensionId, CancellationToken cancellationToken = default);
  Task<Either<List<BatchExtensionDataWithReturnType>, BatchExtensionException>> GetBatchExtensionDataByDaysAsync(CancellationToken cancellationToken = default);
    
  Task<Possible<BatchExtensionException>> AddBatchAsync(List<BatchExtensionData> batchExtensions, CancellationToken cancellationToken = default);
  Task<Possible<BatchExtensionException>> UpdateBatchAsync(List<BatchExtensionData> batchExtensions, Guid batchExtensionId, CancellationToken cancellationToken = default);

  Task<Possible<BatchExtensionException>> UpdateBatchExtensionItemAsync<T>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setProperties,
        CancellationToken cancellationToken = default)
        where T : class;


   Task<Possible<BatchExtensionException>> UpdateBatchExtensionQueueAsync<T>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setProperties,
        CancellationToken cancellationToken = default)
        where T : class;

     Task<Either<List<BatchExtensionDeliverableData>,BatchExtensionException>> GetExtensionDeliverableAsync(CancellationToken cancellationToken = default);

}
