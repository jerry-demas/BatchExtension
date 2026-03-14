using System.Text.Json;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using CBIZ.CCH.BatchExtension.Presentation.BackgroundService;
using Microsoft.AspNetCore.Mvc;

namespace CBIZ.CCH.BatchExtension.API.Endpoints;

public static class BatchEndpoints
{
    public static void MapBatchEndpoints(this IEndpointRouteBuilder app)
    {
        
        app.MapGet("/getbatchstatus/{batchqueueId}", async (
            Guid batchqueueId,
            [FromServices] IBatchService batchService) =>
        {
            return await GetBatchStatusAsync(batchqueueId, batchService);   
        })
        .WithName("getbatchstatusById");

        app.MapGet("/getbatchstatus", async (           
            [FromServices] IBatchService batchService) =>
        {
            return await GetBatchStatusAsync(Guid.Empty, batchService);   
        })
        .WithName("getbatchstatus");
        
        app.MapGet("/getbatchextensiondata", async (            
            [FromServices] IBatchService batchService) =>
        {
            return await GetBatchExtensionDataAsync(batchService);   
        })
        .WithName("getbatchextensiondata");
            
        // /getbatchextensiondata/paged?pageNumber=1&pageSize=50&filterField=ClientNumber&filterField=ClientName&filterValue=test&filterValue=john&sortField=clientName&sortDescending=true
        app.MapGet("/getbatchextensiondata/paged", async (
            [FromServices] IBatchService batchService,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string[]? filterField = null,
            [FromQuery] string[]? filterValue = null,
            [FromQuery] string? sortField = null,
            [FromQuery] bool sortDescending = false
            ) =>
        {
            return await GetBatchExtensionDataPagedAsync(batchService, pageNumber, pageSize, filterField, filterValue, sortField, sortDescending);
        })
        .WithName("getbatchextensiondata_paged");
        
        app.MapPost("/addqueue", static async (
            [FromBody] LaunchBatchRunRequest request,
            [FromServices] IBatchService batchService,
            [FromServices] EmailService emailService,
            [FromServices] BatchQueue batchQueue) =>
        {
            return await AddQueueAsync(request, batchService, emailService, batchQueue);
        })
        .WithName("addqueue");

        app.MapPost("/requeueById/{queueId}", static async (
             Guid queueId,
            [FromServices] IBatchService batchService,
            [FromServices] EmailService emailService,
            [FromServices] BatchQueue batchQueue) =>
        {
            

            try {
                await batchService.UpdateQueueStatusToRequeued(queueId);
                await batchService.UpdateBatchItemsRequeued(queueId);                                  
                var requestRequeue = await GetQueueRequest(queueId, batchService);
                return await AddQueueAsync(requestRequeue, batchService, emailService, batchQueue);
            } catch (Exception ex)
            {
                return Results.Problem(
                detail: ex.Message);
            }

        }).WithName("requeueById");

    }


    private static async Task<LaunchBatchRunRequest> GetQueueRequest(Guid queueId, IBatchService batchService)
    {
        var result = await batchService.GetLaunchBatchQueueRequestByQueueId(queueId);
                 
        if (result.HasFailure)
            throw new BatchExtensionException(result.Failure.Message);
            
        return result.Value;
    }

    private static async Task<IResult> GetBatchExtensionDataAsync(IBatchService batchService)
    {        
        var result = await batchService.GetBatchExtensionData();
        if (result.HasFailure)
        {
            return Results.Problem(
                detail: result.Failure.Message
            );            
        }
        return Results.Ok(result.Value);
    }

    // New helper: server-side paged result with optional filtering and sorting parameters
    private static async Task<IResult> GetBatchExtensionDataPagedAsync(
        IBatchService batchService,
        int pageNumber,
        int pageSize,
        string[]? filterField,
        string[]? filterValue,
        string? sortField,
        bool sortDescending)
    {
        if (pageNumber < 1)
        {
            return Results.Problem(
                detail: "Query parameter 'pageNumber' must be >= 1.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        if (pageSize < 1 || pageSize > 1000)
        {
            return Results.Problem(
                detail: "Query parameter 'pageSize' must be between 1 and 1000.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        // Validate that filterField and filterValue have matching counts
        if (filterField != null && filterValue != null && filterField.Length != filterValue.Length)
        {
            return Results.Problem(
                detail: "The number of 'filterField' parameters must match the number of 'filterValue' parameters.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        if ((filterField != null && filterValue == null) || (filterField == null && filterValue != null))
        {
            return Results.Problem(
                detail: "Both 'filterField' and 'filterValue' must be provided together.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var result = await batchService.GetBatchExtensionDataPaged(pageNumber, pageSize, filterField, filterValue, sortField, sortDescending);
        if (result.HasFailure)
        {
            return Results.Problem(
                detail: result.Failure.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetBatchStatusAsync(Guid batchqueueId, IBatchService batchService)
    {
        var result = await batchService.GetQueueStatus(batchqueueId);
        if (result.HasFailure)
        {
            return Results.Problem(
                detail: result.Failure.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> AddQueueAsync(
        LaunchBatchRunRequest request,
        IBatchService batchService,
        EmailService emailService,
        BatchQueue batchQueue)
    {

         if (request is null)
            return Results.Problem(
                detail: "The request object is missing.",
                statusCode: StatusCodes.Status400BadRequest
            );

        if (request.Returns is null || request.Returns.Count == 0)
            return Results.Problem(
                detail: "No returns were received",
                statusCode: StatusCodes.Status400BadRequest
            );

        if (!request.Returns.Select(r => r.ReturnId).ToList().ValidateReturnIds())
            return Results.Problem(
                detail: "Returns must be of the same return type and same year.",
                statusCode: StatusCodes.Status400BadRequest
            );

        try
        {
            var newQueue = new BatchExtensionQueue
            {                
                QueueRequest = JsonSerializer.Serialize(request),
                QueueStatus = BatchQueueStatus.Scheduled,
                BatchStatus = BatchRecordStatus.Scheduled.Description,
                ReturnType = request.ReturnType,
                SubmittedBy = request.SubmittedBy,
                SubmittedDate = DateTime.Now,
                BatchExtensionData = request.ConvertToBatchExtensionData()
            };
            
            var serviceResponse = await batchService.AddToQueue(newQueue);
            if (serviceResponse.HasFailure)
            {
                await emailService.SendEmailFailureAsync(request.SubmittedBy, $"Adding queue failed: {serviceResponse.Failure.Message}");
                return Results.BadRequest($"Did not work {newQueue}");
            }
            
            var queueItem = new BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>(
                new LaunchBatchQueueRequest(serviceResponse.Value)
            );
            await batchQueue.Writer.WriteAsync(queueItem);
            await emailService.SendEmailSuccessBatchCreatedAsync(request, serviceResponse.Value);
            
            return Results.Ok(new LaunchBatchRunResponse(
                serviceResponse.Value,
                request.SubmittedBy ));          

        } catch (Exception ex)
        {
           
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
