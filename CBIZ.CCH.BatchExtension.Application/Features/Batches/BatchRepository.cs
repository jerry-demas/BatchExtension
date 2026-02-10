
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

    public async Task<Either<List<BatchExtensionDataWithReturnType>, BatchExtensionException>> GetBatchExtensionDataByDaysAsync(int days, CancellationToken cancellationToken = default)
    {
        try
        {                       
            var cutoff = DateTime.UtcNow.AddDays(-days);

            return await _dbContext.BatchQueue
                .AsNoTracking()
                .Where(q => q.BatchExtensionData.Any(d => d.CreationDate >= cutoff))
                .SelectMany(q => q.BatchExtensionData
                    .Where(d => d.CreationDate >= cutoff)
                    .Select(d => new BatchExtensionDataWithReturnType
                    {
                        Id = d.Id,
                        QueueIDGUID = d.QueueIDGUID,
                        FirmFlowId = d.FirmFlowId,
                        TaxReturnId = d.TaxReturnId,
                        ReturnType = q.ReturnType,
                        ClientName = d.ClientName,
                        ClientNumber = d.ClientNumber,
                        OfficeLocation = d.OfficeLocation,
                        EngagementType = d.EngagementType,
                        BatchId = d.BatchId,
                        BatchItemGuid = d.BatchItemGuid,
                        BatchItemStatus = d.BatchItemStatus,
                        StatusDescription = d.StatusDescription,
                        FileName = d.FileName,
                        FileDownLoadedFromCCH = d.FileDownLoadedFromCCH,
                        FileUploadedToGFR = d.FileUploadedToGFR,
                        GfrDocumentId = d.GfrDocumentId,
                        CreationDate = d.CreationDate,
                        UpdatedDate = d.UpdatedDate
                    })
                )
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

    public async Task<Either<List<BatchQueueStatusResponse>, BatchExtensionException>> GetQueueStatus(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {            
            
            var returnList = await _dbContext.BatchQueue
                .Include(q => q.BatchExtensionData)                
                .Select(q => new BatchQueueStatusResponse(
                    q.QueueId,                    
                    q.SubmittedBy,
                    q.SubmittedDate,
                    q.QueueStatus,
                    q.ReturnType,
                    q.BatchExtensionData                        
                        .Select(d =>  new BatchExtensionData
                        {
                            Id = d.Id,
                            QueueIDGUID = d.QueueIDGUID,
                            FirmFlowId = d.FirmFlowId,
                            TaxReturnId = d.TaxReturnId,
                            ClientName = d.ClientName,
                            ClientNumber = d.ClientNumber,
                            OfficeLocation = d.OfficeLocation,
                            EngagementType = d.EngagementType,
                            BatchId = d.BatchId,
                            BatchItemGuid = d.BatchItemGuid,
                            BatchItemStatus = d.BatchItemStatus,
                            StatusDescription = d.StatusDescription,
                            FileName = d.FileName,
                            FileDownLoadedFromCCH = d.FileDownLoadedFromCCH,
                            FileUploadedToGFR = d.FileUploadedToGFR,
                            GfrDocumentId = d.GfrDocumentId,
                            Message = d.Message,
                            CreationDate = d.CreationDate,
                            UpdatedDate = d.UpdatedDate
                    }).ToList()))
                .ToListAsync(cancellationToken);
            
            
            if (returnList is null || returnList.Count == 0)
                return new BatchExtensionException($"No queues found for queueId {queueId}.");

            if(!queueId.Equals(Guid.Empty)){
                returnList = returnList.Where(q => q.QueueId == queueId).ToList();            
            }

            return returnList;
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
            await _dbContext.SaveChangesAsync(cancellationToken);
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
            _logger.LogError(ex, "Error updating entity {EntityType}", typeof(T).Name);
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
            _logger.LogError(ex, "Error updating entity {EntityType}", typeof(T).Name);
            return new BatchExtensionException($"Error updating {typeof(T).Name}", ex);
        }
    }

    public async Task<Either<List<BatchExtensionDeliverableData>,BatchExtensionException>> GetExtensionDeliverableAsync(       
        CancellationToken cancellationToken = default)
    {
        try
        {                        
            var deliverableReturn = await _dbContext.Deliverables               
                .AsNoTracking()    
                .ToListAsync(cancellationToken);

            if (deliverableReturn is null)
            {
                return new BatchExtensionException($"Deliverables were not found.");
            }

            return deliverableReturn;

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetExtensionDeliverableAsync");
            return new BatchExtensionException($"Error getting record in GetExtensionDeliverableAsync ", ex);
        }
    }




}

