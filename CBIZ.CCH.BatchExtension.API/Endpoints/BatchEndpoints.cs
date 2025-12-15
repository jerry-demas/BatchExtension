using System.Text.Json;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;
using CBIZ.CCH.BatchExtension.Presentation.BackgroundService;

using Microsoft.AspNetCore.Http.HttpResults;
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
        
        app.MapPost("/addqueue", static async (
            [FromBody] LaunchBatchRunRequest request,
            [FromServices] IBatchService batchService,
            [FromServices] EmailService emailService,
            [FromServices] BatchQueue batchQueue) =>
        {
            return await AddQueueAsync(request, batchService, emailService, batchQueue);
        })
        .WithName("addqueue");
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

        if (request.Returns == null || request.Returns.Count == 0)
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
                QueueId = Guid.Empty,               
                QueueRequest = JsonSerializer.Serialize(request),
                QueueStatus = BatchQueueStatus.Scheduled,
                BatchStatus = BatchRecordStatus.Scheduled.Description,
                ReturnType = request.ReturnType,
                SubmittedBy = request.SubmittedBy,
                SubmittedDate = DateTime.Now
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
