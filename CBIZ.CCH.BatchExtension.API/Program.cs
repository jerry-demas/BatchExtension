using CBIZ.CCH.BatchExtension.Presentation.BackgroundService;
using CBIZ.CCH.BatchExtension.Application;
using Microsoft.AspNetCore.Mvc;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.API;
using System.Text.Json;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.SharedPackages.Mail;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMailService();

builder.AddProgramServiceLayer(builder.Configuration)
       .AddApplicationServiceLayer(builder.Configuration)
       .AddBackgroundServiceLayer(builder.Configuration);

builder.Services.AddSingleton<BatchQueue>()                       
                .AddSingleton<EmailService>()                              
                .AddHostedService<Worker>();


var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapGet("/getbatchstatus/{batchqueueId}", async (
    Guid batchqueueId,
    [FromServices] IBatchService batchService) =>
    {
        try
        {
           
            var result = await batchService.GetQueueStatus(batchqueueId);
            return result.Match(
                successValue => Results.Ok(successValue),
                failureValue => Results.Problem(
                    detail: failureValue.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                )
            );
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }).WithName("getbatchstatus");


app.MapPost("/addqueue", static async (
    [FromBody] LaunchBatchRunRequest request,
    [FromServices] IBatchService batchService,
    [FromServices] EmailService emailService,
    [FromServices] BatchQueue batchQueue) =>
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

    List<string> items = request.Returns.Select(r => r.ReturnId).ToList();
    if (!items.ValidateReturnIds())
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
            SubmittedBy = request.SubmittedBy,
            SubmittedDate = DateTime.UtcNow
        };

        var serviceResponse = await batchService.AddToQueue(newQueue);
        return await serviceResponse.Match(
            async successValue =>
            {
                await emailService.SendSuccessEmailAsync($"Yeah: {successValue}");
                var queueItem = new BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>(
                    new LaunchBatchQueueRequest(successValue)
                );
                await batchQueue.Writer.WriteAsync(queueItem);
                return Results.Ok($"QueueId:{successValue}");
            },
            async failureValue =>
            {
                await emailService.SendFalureEmailAsync($"Boo {failureValue.Message}");
                return Results.BadRequest($"Did not work {newQueue}");
            }
        );

    }
    catch (Exception ex)
    {
        return Results.Problem(
           detail: ex.Message,
           statusCode: StatusCodes.Status500InternalServerError
       );
    }
})
.WithName("addqueue");





app.MapPost("/runbatch",  async ([FromBody] LaunchBatchRunRequest request, BatchQueue batchQueue) =>
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
    /*
    var queueItem = new BatchQueueItem<LaunchBatchRunRequest, LaunchBatchRunResponse>(request);
    await batchQueue.Writer.WriteAsync(queueItem);
    try
    {
        var response = await queueItem.Tcs.Task;
        return Results.Ok(response);

    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }    
    */
    await Task.Delay(0);
    return Results.Ok($"ended"); 
})
.WithName("RunBatch");


await app.RunAsync();




