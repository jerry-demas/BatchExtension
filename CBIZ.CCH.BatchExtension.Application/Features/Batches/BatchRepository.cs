
using System.Linq;
using System.Linq.Expressions;

using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public class BatchRepository(
    BatchDbContext context,
    ILogger<BatchRepository> logger) : IBatchRepository
{
    private readonly BatchDbContext _dbContext = context;
    private readonly ILogger<BatchRepository> _logger = logger;

    public async Task<Either<List<BatchExtensionData>, BatchExtensionException>> GetBatchAsync(Guid batchExtensionId, CancellationToken cancellationToken = default)
    {

        try
        {
            if (batchExtensionId.Equals(Guid.Empty)) return new BatchExtensionException($"BatchGuidId is missing");
            return await _dbContext.Batches
                .Where(_ => batchExtensionId.Equals(_.BatchId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in AddBatchAsync");
            return new BatchExtensionException($"Error getting batch in GetBatchAsync ", ex);
        }


    }

    public async Task<Possible<BatchExtensionException>> AddBatchAsync(List<BatchExtensionData> batchExtensions, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbContext.Batches.AddRange(batchExtensions);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Possible.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in AddBatchAsync");
            return new BatchExtensionException($"Error adding to the database in AddBatchAsync ", ex);
        }
    }

    public async Task<Either<BatchQueueStatusResponse, BatchExtensionException>> GetQueueStatus(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dbContext.BatchQueue
                .Where(_ => _.QueueId == queueId)
                .AsNoTracking()
                .Select(x => new BatchQueueStatusResponse(x.QueueId, x.QueueStatus))
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
                return new BatchExtensionException($"Queue ID {queueId} not found.");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetQueueStatus");
            return new BatchExtensionException($"Error getting batch in GetQueueStatus ", ex);
        }

    }

    public async Task<Possible<BatchExtensionException>> UpdateBatchAsync(List<BatchExtensionData> batchExtensions, Guid batchExtensionId, CancellationToken cancellationToken = default)
    {
        try
        {

            foreach (var batchExtensionItem in batchExtensions)
            {

                var dbToUpdate = batchExtensionItem.Id > Guid.Empty
                        ? await _dbContext.Batches.FirstOrDefaultAsync(_ => _.Id == batchExtensionItem.Id, cancellationToken)
                        : await _dbContext.Batches.FirstOrDefaultAsync(_ =>
                                    _.FirmFlowId == batchExtensionItem.FirmFlowId &&
                                    _.TaxReturnId == batchExtensionItem.TaxReturnId, cancellationToken);

                if (dbToUpdate is not null)
                {
                    batchExtensionItem.UpdateExtensionDataDbFrom(batchExtensionItem);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

            }

            return Possible.Completed;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateBatchAsync");
            return new BatchExtensionException($"Error updating the database in UpdateBatchAsync ", ex);
        }
    }


    public async Task<Either<Guid, BatchExtensionException>> AddToBatchQueue(BatchExtensionQueue batchQueue, CancellationToken cancellationToken = default)
    {

        try
        {

            _dbContext.BatchQueue.Add(batchQueue);
            var retGuid = await _dbContext.SaveChangesAsync(cancellationToken);
            return batchQueue.QueueId;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in AddToBatchQueue");
            return new BatchExtensionException($"Error adding to the database in AddToBatchQueue {batchQueue}", ex);
        }
    }

    public async Task<Either<BatchExtensionQueue, BatchExtensionException>> GetBatchQueueById(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (queueItemId.Equals(Guid.Empty)) return new BatchExtensionException($"QueueItemId is missing");
            var queueItem = await _dbContext.BatchQueue
                .Where(_ => _.QueueId == queueItemId)
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            if (queueItem is null) return new BatchExtensionException($"QueueItemId {queueItemId} not found");
            return queueItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchQueueById");
            return new BatchExtensionException($"Error getting batch in GetBatchQueueById : {queueItemId} ", ex);
        }


    }
    
    /*    public async Task<Possible<BatchExtensionException>> UpdateQueueAsync(BatchExtensionQueue batchQueue, CancellationToken cancellationToken = default)
    {

        try
        {
            var queueItem = await _dbContext.BatchQueue
                        .FirstOrDefaultAsync(_ => _.QueueId == batchQueue.QueueId, cancellationToken);

            if (queueItem is not null)
            {
                batchQueue.UpdateExtensionQueueDbFrom(batchQueue);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return Possible.Completed;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateQueueAsync");
            return new BatchExtensionException($"Error updating the database in UpdateQueueAsync Id {batchQueue.QueueId} ", ex);
        }

    }
    */

/*
    public async Task<Possible<BatchExtensionException>> UpdateQueueStatusAsync(Guid queueId, string status, CancellationToken cancellationToken = default)
    {
        try
        {

            await _dbContext.BatchQueue
                .Where(x => x.QueueId == queueId)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(b => b.QueueStatus, b => status),
                    cancellationToken);

            return Possible.Completed;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateQueueStatusAsync");
            return new BatchExtensionException($"Error updating the database in UpdateQueueStatusAsync Id {queueId} ", ex);

        }

    }
*/


    public async Task<Possible<BatchExtensionException>> UpdateBatchExtensionItemAsync<T>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setProperties,
        CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            await _dbContext.Set<T>()
                .Where(predicate)
                .ExecuteUpdateAsync(setProperties, cancellationToken);

            return Possible.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating entity {typeof(T).Name}");
            return new BatchExtensionException($"Error updating {typeof(T).Name}", ex);
        }
    }



 public async Task<Possible<BatchExtensionException>> UpdateBatchExtensionQueueAsync<T>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setProperties,
        CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            await _dbContext.Set<T>()
                .Where(predicate)
                .ExecuteUpdateAsync(setProperties, cancellationToken);

            return Possible.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating entity {typeof(T).Name}");
            return new BatchExtensionException($"Error updating {typeof(T).Name}", ex);
        }
    }



}

