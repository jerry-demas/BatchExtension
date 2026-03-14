using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Infrastructure;
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
    
    public async Task<Either<LaunchBatchRunRequest, BatchExtensionException>> GetLaunchBatchRequestByqueueId(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (queueId.Equals(Guid.Empty)) return new BatchExtensionException($"QueueId is missing");
            
            var result = await _dbContext.BatchQueue
                .Where(_ => _.QueueId == queueId)
                .Select(_ => _.QueueRequest)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(result))
                 return new BatchExtensionException($"No queue found for queueId {queueId}");


           var request = JsonSerializer.Deserialize<LaunchBatchRunRequest>(
                result,
                JsonDefaults.jsonOptions);

            if (request == null)
            {
                return new BatchExtensionException("Queue request JSON could not be deserialized");
            }

            return request;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchByQueueIdAsync");
            return new BatchExtensionException($"Error getting batch in GetBatchByQueueIdAsync ", ex);
        }
    }

    public async Task<Either<List<BatchExtensionData>, BatchExtensionException>> GetBatchExtensionDataByQueueId(Guid queueId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (queueId.Equals(Guid.Empty)) return new BatchExtensionException($"QueueId is missing");
            return await _dbContext.Batches               
                .Where(_ => queueId == _.QueueIDGUID)                
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchByQueueIdAsync");
            return new BatchExtensionException($"Error getting batch in GetBatchByQueueIdAsync ", ex);
        }
    }

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

    public async Task<Either<List<BatchExtensionDataWithReturnType>, BatchExtensionException>> GetBatchExtensionDataByDaysAsync(CancellationToken cancellationToken = default)
    {
        try
        {                                   
            return await _dbContext.BatchQueue
                .AsNoTracking()                
                .SelectMany(q => q.BatchExtensionData                   
                    .Select(d => new BatchExtensionDataWithReturnType
                    {
                        Id = d.Id,
                        QueueIDGUID = d.QueueIDGUID,
                        FirmFlowId = d.FirmFlowId,
                        TaxReturnId = d.TaxReturnId,
                        Pic = d.Pic,
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
                        UpdatedDate = d.UpdatedDate,
                        SubmittedBy = q.SubmittedBy,
                        Message = d.Message,
                    })
                )
                .ToListAsync(cancellationToken);            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchExtensionDataByDaysAsync");
            return new BatchExtensionException($"Error getting batch in GetBatchExtensionDataByDaysAsync ", ex);
        }
    }


    public async Task<Either<PagedResult<BatchExtensionDataWithReturnType>, BatchExtensionException>> GetBatchExtensionDataPagedAsync(
        int page,
        int pageSize,
        string[]? filterField = null,
        string[]? filterValue = null,
        string? sortField = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
        => await GetBatchExtensionDataPagedAsyncCore(
            page,
            pageSize,
            filters: BuildFilterPairs(filterField, filterValue),
            sortField,
            sortDescending,
            cancellationToken);

    private static (string Field, string Value)[]? BuildFilterPairs(string[]? fields, string[]? values)
    {
        if (fields == null || values == null || fields.Length == 0 || values.Length == 0)
            return null;

        if (fields.Length != values.Length)
            throw new BatchExtensionException("filterField and filterValue arrays must have the same length.");

        var list = new List<(string Field, string Value)>(fields.Length);
        for (int i = 0; i < fields.Length; i++)
        {
            var f = fields[i];
            var v = values[i];
            if (string.IsNullOrWhiteSpace(f) || string.IsNullOrWhiteSpace(v))
                continue;
            list.Add((f!, v!));
        }

        return list.Count == 0 ? null : list.ToArray();
    }

    private async Task<Either<PagedResult<BatchExtensionDataWithReturnType>, BatchExtensionException>> GetBatchExtensionDataPagedAsyncCore(
        int page,
        int pageSize,
        (string Field, string Value)[]? filters,
        string? sortField,
        bool sortDescending,
        CancellationToken cancellationToken)
    {
        try
        {
            if (page < 1) return new BatchExtensionException("Page must be >= 1");
            if (pageSize < 1) return new BatchExtensionException("PageSize must be >= 1");

            var baseQuery = BuildBaseQuery();

            if (filters != null)
            {
                foreach (var (field, value) in filters)
                {
                    var propertyInfo = GetPropertyInfo(field);
                    if (propertyInfo == null)
                        return new BatchExtensionException($"Filter field '{field}' does not exist on type {nameof(BatchExtensionDataWithReturnType)}.");

                    baseQuery = ApplyFilter(baseQuery, propertyInfo, value, field);
                }
            }

            if (!string.IsNullOrWhiteSpace(sortField))
            {
                var propertyInfo = GetPropertyInfo(sortField);
                if (propertyInfo == null)
                    return new BatchExtensionException($"Sort field '{sortField}' does not exist on type {nameof(BatchExtensionDataWithReturnType)}.");

                baseQuery = ApplySort(baseQuery, propertyInfo, sortDescending);
            }
            else
            {
                baseQuery = baseQuery.OrderBy(i => i.Id);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<BatchExtensionDataWithReturnType>(items, page, pageSize, totalCount, totalPages);
        }
        catch (BatchExtensionException bex)
        {
            // preserve semantic of returning domain exception
            _logger.LogWarning(bex, "Filter validation failed in GetBatchExtensionDataPagedAsync");
            return bex;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetBatchExtensionDataPagedAsync");
            return new BatchExtensionException($"Error getting batch in GetBatchExtensionDataPagedAsync ", ex);
        }
    }

    private IQueryable<BatchExtensionDataWithReturnType> BuildBaseQuery()
    {
        return from q in _dbContext.BatchQueue.AsNoTracking()
               from d in q.BatchExtensionData
               select new BatchExtensionDataWithReturnType
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
                   UpdatedDate = d.UpdatedDate,
                   SubmittedBy = q.SubmittedBy
               };
    }

    private static PropertyInfo? GetPropertyInfo(string propertyName)
    {
        return typeof(BatchExtensionDataWithReturnType)
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    }

    private static IQueryable<BatchExtensionDataWithReturnType> ApplyFilter(
        IQueryable<BatchExtensionDataWithReturnType> query,
        PropertyInfo propertyInfo,
        string filterValue,
        string filterField)
    {
        var propertyType = propertyInfo.PropertyType;
        var filterContext = new FilterContext(query, propertyInfo, filterValue, filterField, propertyType);

        return GetFilterStrategy(propertyType)(filterContext);
    }

    private static Func<FilterContext, IQueryable<BatchExtensionDataWithReturnType>> GetFilterStrategy(Type propertyType)
    {
        if (propertyType == typeof(string))
            return ctx => ApplyStringFilter(ctx.Query, ctx.PropertyInfo, ctx.FilterValue);

        if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
            return ctx => ApplyGuidFilter(ctx.Query, ctx.PropertyInfo, ctx.FilterValue, ctx.FilterField);

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            return ctx => ApplyDateFilter(ctx.Query, ctx.PropertyInfo, ctx.FilterValue, ctx.FilterField);

        if (propertyType == typeof(bool) || propertyType == typeof(bool?))
            return ctx => ApplyBooleanFilter(ctx.Query, ctx.PropertyInfo, ctx.FilterValue, ctx.FilterField);

        return ctx => ApplyNumericFilter(ctx.Query, ctx.PropertyInfo, ctx.FilterValue, ctx.FilterField, ctx.PropertyType);
    }

    private readonly record struct FilterContext(
        IQueryable<BatchExtensionDataWithReturnType> Query,
        PropertyInfo PropertyInfo,
        string FilterValue,
        string FilterField,
        Type PropertyType);

    private static IQueryable<BatchExtensionDataWithReturnType> ApplyStringFilter(
        IQueryable<BatchExtensionDataWithReturnType> query,
        PropertyInfo propertyInfo,
        string filterValue)
    {
        return query.Where(e => EF.Functions.Like(
            EF.Property<string>(e, propertyInfo.Name).ToLower(),
            $"%{filterValue.ToLower()}%"));
    }

    private static IQueryable<BatchExtensionDataWithReturnType> ApplyGuidFilter(
        IQueryable<BatchExtensionDataWithReturnType> query,
        PropertyInfo propertyInfo,
        string filterValue,
        string filterField)
    {
        if (!Guid.TryParse(filterValue, out var guidValue))
            throw new BatchExtensionException($"Invalid GUID value '{filterValue}' for filter field '{filterField}'.");

        return query.Where(e => EF.Property<Guid?>(e, propertyInfo.Name) == guidValue);
    }

    private static IQueryable<BatchExtensionDataWithReturnType> ApplyBooleanFilter(
        IQueryable<BatchExtensionDataWithReturnType> query,
        PropertyInfo propertyInfo,
        string filterValue,
        string filterField)
    {
        if (!bool.TryParse(filterValue, out var boolValue))
            throw new BatchExtensionException($"Invalid boolean value '{filterValue}' for filter field '{filterField}'.");

        return query.Where(e => EF.Property<bool?>(e, propertyInfo.Name) == boolValue);
    }

    private static IQueryable<BatchExtensionDataWithReturnType> ApplyNumericFilter(
        IQueryable<BatchExtensionDataWithReturnType> query,
        PropertyInfo propertyInfo,
        string filterValue,
        string filterField,
        Type propertyType)
    {
        try
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var converted = Convert.ChangeType(filterValue, underlyingType);

            var parameter = Expression.Parameter(typeof(BatchExtensionDataWithReturnType), "e");
            var member = Expression.Property(parameter, propertyInfo);
            var constant = Expression.Constant(converted, underlyingType);
            
            var comparison = BuildNumericComparison(member, constant, propertyType, underlyingType);
            var lambda = Expression.Lambda<Func<BatchExtensionDataWithReturnType, bool>>(comparison, parameter);
            
            return query.Where(lambda);
        }
        catch
        {
            throw new BatchExtensionException($"Unsupported filter field: {filterField}, value: {filterValue}, type: {propertyType.Name}");
        }
    }

    private static BinaryExpression BuildNumericComparison(
        MemberExpression member,
        ConstantExpression constant,
        Type propertyType,
        Type underlyingType)
    {
        if (propertyType != underlyingType) // Nullable type
        {
            var hasValue = Expression.Property(member, "HasValue");
            var valueProp = Expression.Property(member, "Value");
            var equals = Expression.Equal(valueProp, constant);
            return Expression.AndAlso(hasValue, equals);
        }
        
        return Expression.Equal(member, Expression.Convert(constant, propertyType));
    }

    private static IQueryable<BatchExtensionDataWithReturnType> ApplyDateFilter(
        IQueryable<BatchExtensionDataWithReturnType> query,
        PropertyInfo propertyInfo,
        string filterValue,
        string filterField)
    {
        string dateValueString = filterValue.Trim();

        if (!DateTime.TryParse(dateValueString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dateValue))
            throw new BatchExtensionException($"Invalid DateTime value '{filterValue}' for filter field '{filterField}'.");

        // Normalize to date only (midnight)
        var dateOnly = dateValue.Date;

        // Check if the property is nullable or non-nullable DateTime
        var isNullable = Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null;

        // Use EF.Property with the correct type based on nullability
        // Compare only the date part using EF.Functions.DateDiffDay
        if (isNullable)
        {
            return query.Where(e => EF.Property<DateTime?>(e, propertyInfo.Name).HasValue &&
                                   EF.Property<DateTime?>(e, propertyInfo.Name)!.Value.Date == dateOnly);
        }
        else
        {
            return query.Where(e => EF.Property<DateTime>(e, propertyInfo.Name).Date == dateOnly);
        }
    }

    private static IQueryable<BatchExtensionDataWithReturnType> ApplySort(
        IQueryable<BatchExtensionDataWithReturnType> query,
        PropertyInfo propertyInfo,
        bool descending)
    {
        var parameter = Expression.Parameter(typeof(BatchExtensionDataWithReturnType), "e");
        var property = Expression.Property(parameter, propertyInfo);
        var lambda = Expression.Lambda(property, parameter);

        var methodName = descending ? "OrderByDescending" : "OrderBy";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(BatchExtensionDataWithReturnType), propertyInfo.PropertyType],
            query.Expression,
            Expression.Quote(lambda));

        return query.Provider.CreateQuery<BatchExtensionDataWithReturnType>(resultExpression);
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


    public Task<Either<List<Guid>, BatchExtensionException>> GetScheduledBatchQueueIds(CancellationToken cancellationToken = default)
        => GetBatchQueueIdsByStatus( BatchQueueStatus.Scheduled, cancellationToken);

    private async Task<Either<List<Guid>, BatchExtensionException>> GetBatchQueueIdsByStatus(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.BatchQueue
                .Where(q => q.BatchStatus == status && q.BatchExtensionData.Any())
                .AsNoTracking()
                .OrderBy(q => q.SubmittedDate)
                .Select(q => q.QueueId)
                .ToListAsync(cancellationToken);

        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetScheduledBatchQueueIds");
            return new BatchExtensionException($"Error getting Scheduled queues in GetScheduledBatchQueueIds ", ex);
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
                            Pic = d.Pic,
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
        string queueReturnType,       
        CancellationToken cancellationToken = default)
    {
        try
        {   

            var query = _dbContext.Deliverables.AsNoTracking();
            if(queueReturnType == BatchExtensionConstants.TaxReturnTypes.Federal)
            {
                query = query.Where(_ => _.Jurisdiction == BatchExtensionConstants.Jurisdiction.Federal);
            }
            var deliverableReturn = await query.ToListAsync(cancellationToken);
                      
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

