

using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using Azure.Core;

using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace CBIZ.CCH.BatchExtension.Presentation.BackgroundService;



public sealed class Worker : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<Worker> _logger;

    
    public Worker(
        IServiceProvider services,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger )
    {
        _services = services;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;      
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Worker starting at: {DateTimeOffset.Now}");

        using IServiceScope serviceScope = _services.CreateScope();

        BatchQueue batchQueue = serviceScope.ServiceProvider.GetRequiredService<BatchQueue>();

        ICchService cchService = serviceScope.ServiceProvider.GetRequiredService<ICchService>();

        IGfrService gfrService = serviceScope.ServiceProvider.GetRequiredService<IGfrService>();

        IBatchRepository batchRepository = serviceScope.ServiceProvider.GetRequiredService<IBatchRepository>();
        IBatchService batchService = serviceScope.ServiceProvider.GetRequiredService<IBatchService>();
        //IEmailService emailService = serviceScope.ServiceProvider.GetRequiredService<IEmailService>();

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid batchGuidId = Guid.Empty;
            Guid queueId = Guid.Empty;

            var reader = batchQueue.Reader;
            try
            {
                var batchExtensionQueueItem = new List<BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>>();
                if (await reader.WaitToReadAsync(stoppingToken))
                    while (reader.TryRead(out var queue))
                    {
                        Console.WriteLine(queue);                        
                        batchExtensionQueueItem.Add(queue);
                    }

                //Get the queueItem and insert into  BatchExtensionData
                try
                {
                
                    LaunchBatchRunRequest request = new LaunchBatchRunRequest();
                    List<BatchExtensionData> batch = new List<BatchExtensionData>();
                    queueId = batchExtensionQueueItem[0].Request.QueueId;

                    //
                    /*
                    GetBatchQueue(batchRepository, queueId)
                        .ContinueWith(
                            UpdateQueueStatusToRunning(batchService, queueId)
                                .ContinueWith(
                                    CreateBatch(cchService, queueId, stoppingToken)
                                )
                    );
                    */
                    GetBatchQueue(batchRepository, queueId).Result
                        .Match(
                            success =>
                            {
                                request = success ?? new LaunchBatchRunRequest();
                                UpdateQueueStatusToRunning(batchService, queueId).Result
                                    .Match(
                                        success =>
                                        {
                                            CchCreateBatch(cchService, request, stoppingToken).Result
                                                .Match(
                                                    success =>
                                                    {
                                                        batchGuidId = success;
                                                        batch.AddRange(request.Returns.ConvertToBatchExtensionData(queueId, batchGuidId));
                                                        AddBatchToDB(batchRepository, batch, stoppingToken).Result
                                                        .WhenFailure(
                                                            failure =>
                                                            {
                                                                LogErrorMessage(failure.Message);
                                                                batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error adding batches to the database : {failure.Message}"));
                                                                return;
                                                            }
                                                        );

                                                        GetBatchStatus(cchService, batchGuidId, batch.Count(), stoppingToken).Result
                                                        .Match(
                                                            success =>
                                                            {
                                                                if (success.Equals(BatchRecordStatus.Exception.Description))
                                                                {
                                                                    LogErrorMessage($"Batch Status {success} issue for batchId {batchGuidId}");
                                                                    batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error getting status for batch {batchGuidId}"));
                                                                    return;
                                                                }
                                                                CreateCCHBatchFiles(cchService, batch, batchGuidId, stoppingToken).Result
                                                                    .Match(
                                                                        success =>
                                                                        {
                                                                            UpdateBatchDataToDB(batchRepository, batch, batchGuidId, stoppingToken).Result
                                                                            .WhenFailure(
                                                                               failure =>
                                                                                {
                                                                                    LogErrorMessage(failure.Message);
                                                                                    batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException(failure.Message));
                                                                                    return;
                                                                                }
                                                                            );
                                                                            DownloadCCHFileAndUploadToGFR(cchService,
                                                                                batchService,
                                                                                gfrService,
                                                                                batch,
                                                                                stoppingToken).Result
                                                                                .Match(success => { },
                                                                                       failure =>
                                                                                       {
                                                                                           LogErrorMessage(failure.Message);
                                                                                           batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException(failure.Message));
                                                                                           return;
                                                                                       }
                                                                                );
                                                                        },
                                                                        failure =>
                                                                        {
                                                                            LogErrorMessage(failure.Message);
                                                                            batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException(failure.Message));
                                                                            return;
                                                                        }
                                                                    );
                                                            },
                                                            failure =>
                                                            {
                                                                LogErrorMessage(failure.Message);
                                                                batchService.UpdateQueueStatusToCompleteWithErrors(queueId);
                                                                batchService.UpdateBatchStatusFailed(batchGuidId);
                                                                batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error getting status for batch {batchGuidId} : {failure.Message}"));
                                                                return;
                                                            }
                                                        );
                                                    },
                                                    failure =>
                                                    {
                                                        LogErrorMessage(failure.Message);
                                                        batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"{failure.Message}"));
                                                        return;
                                                    }
                                                );
                                        },
                                     failure =>
                                     {
                                         LogErrorMessage($"Updating error status for queue {queueId}");
                                         batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Updating error status for queue {queueId}"));
                                         return;
                                     }
                                 );

                                Console.WriteLine(success);

                            },
                            failure => LogErrorMessage(failure.Message)

                        );


                    #region commentedout      
                    /*
                    
                    var batchQueueItem = await batchRepository.GetBatchQueueById(queueId);
                    batchQueueItem.Match(
                        success =>
                        {
                            BatchExtensionQueue queueItem = success;
                            request = JsonSerializer.Deserialize<LaunchBatchRunRequest>(queueItem.QueueRequest)
                                ?? new LaunchBatchRunRequest();
                        },
                        failure =>
                        {
                            LogErrorMessage(failure.Message);
                            batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error processing batch : {failure.Message}"));
                            return;
                        }
                    );

                    var updateStatus = await batchService.UpdateQueueStatusToRunning(queueId);
                    if (updateStatus.HasFailure)
                    {
                        LogErrorMessage($"Updating error status for queue {queueId}");
                        batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Updating error status for queue {queueId}"));
                        return;
                    }

                    // Create the batch via CCH
                    List<string> items = request.Returns.Select(r => r.ReturnId).Distinct().ToList();
                    var executionId = await cchService.CreateBatchAsync(request.ReturnType, items, stoppingToken);
                    executionId.Match(
                        success =>
                        {
                            batchGuidId = Guid.TryParse(success, out var parsed) ? parsed : Guid.Empty;
                            batch.AddRange(request.Returns.ConvertToBatchExtensionData(queueId, batchGuidId));
                        },
                        failure =>
                        {
                            LogErrorMessage(failure.Message);
                            batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error processing batch : {failure.Message}"));
                            return;
                        }
                    );

                    //Add the batches to the database
                    var batchResult = await batchRepository.AddBatchAsync(batch, stoppingToken);
                    batchResult.WhenFailure(failure =>
                    {
                        LogErrorMessage(failure.Message);
                        batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error adding batches to the database : {failure.Message}"));
                        return;
                    });

                    //Check for the Status                   
                    var statusCheck = await cchService.GetBatchStatusAsync(batchGuidId, batch.Count(), stoppingToken);
                    statusCheck.Match(
                        success =>
                        {
                            if (success.Equals(BatchRecordStatus.Exception.Description))
                            {
                                LogErrorMessage($"Batch Status {success} issue for batchId {batchGuidId}");
                                batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error getting status for batch {batchGuidId}"));
                                return;
                            }

                        },
                        failure =>
                        {
                            LogErrorMessage(failure.Message);
                            batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error getting status for batch {batchGuidId} : {failure.Message}"));
                            return;
                        }
                    );


                    //Call to set the documents to be created in CCH
                    var batchCreatOutputFiles = await cchService.CreateBatchOutputFilesAsync(batchGuidId);
                    batchCreatOutputFiles.Match(
                        batch.UpdateBatchItemsGuidAndFileName,
                        failure =>
                        {
                            LogErrorMessage(failure.Message);
                            batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error getting status for batch {batchGuidId} : {failure.Message}"));
                            return;
                        }
                    );

                    //Update the database
                    var batchRepositoryResponse = await batchRepository.UpdateBatchAsync(batch, batchGuidId, stoppingToken);
                    //Now download the documents

                    foreach (var batchItemDocument in batch)
                    {

                        var downloadResult = await cchService.DownloadBatchOutputFilesAsync(
                                batchItemDocument.BatchId,
                                batchItemDocument.BatchItemGuid,
                                batchItemDocument.FileName ?? string.Empty);

                        downloadResult.Match(
                            async success =>
                            {
                                await batchService.UpdateBatchItemDownloadedFromCCH(batchItemDocument.BatchItemGuid);

                                //Upload the files to GFR
                                var gfrUpload = await gfrService.UploadDocumentToGfr(batchItemDocument.TaxReturnId,
                                                                                     batchItemDocument.FirmFlowId,
                                                                                     batchItemDocument.FileName ?? string.Empty);
                                gfrUpload.Match(
                                    async success =>
                                    {
                                        var updateResult = await batchService.UpdateBatchItemUploadedToGFR(batchItemDocument.BatchItemGuid);
                                        if (updateResult.HasFailure)
                                            LogErrorMessage("Failed saving uploading to GFR flag");
                                    },
                                    failure => LogErrorMessage(failure.Message)
                                );

                            },
                            failure =>
                            {
                                LogErrorMessage(failure.Message);
                            }
                        );
                        Console.WriteLine("");
                    }

                    await batchService.UpdateQueueStatusToComplete(queueId);
                    batchExtensionQueueItem[0].Tcs.SetResult(new LaunchBatchQueueResponse("Process Complete"));
                    */
                    #endregion end

                }
                catch (Exception ex)
                {
                    foreach (var q in batchExtensionQueueItem) q.Tcs.SetException(ex);
                    Console.WriteLine(ex);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

    
        }
        



        Console.WriteLine($"Worker stopping at: {DateTimeOffset.Now}");

    }


    
    private async Task<Either<LaunchBatchRunRequest, BatchExtensionException>> GetBatchQueue(IBatchRepository repo, Guid queueId)
    {
        LaunchBatchRunRequest? batchRequest = null;
        BatchExtensionException? exception = null;

        var batchQueueItem = await repo.GetBatchQueueById(queueId);
        batchQueueItem.Match(
           success =>
            {
                BatchExtensionQueue queueItem = success;
                batchRequest = JsonSerializer.Deserialize<LaunchBatchRunRequest>(queueItem.QueueRequest)
                    ?? new LaunchBatchRunRequest();
            },
            failure =>
            {
                LogErrorMessage(failure.Message);
                //batchExtensionQueueItem[0].Tcs.SetException(new BatchExtensionException($"Error processing batch : {failure.Message}"));
                exception = new BatchExtensionException($"Error processing batch : {failure.Message}");
            }
        );

        if (batchRequest is not null)
            return batchRequest;

        if (exception is not null)
            return exception;

        return new BatchExtensionException($"Value could not be found");
    }

    private async Task<Possible<BatchExtensionException>> UpdateQueueStatusToRunning(IBatchService service, Guid queueId)
    {        
        var updateStatus = await service.UpdateQueueStatusToRunning(queueId);
        if (updateStatus.HasFailure)
        {
            //LogErrorMessage($"Updating error status for queue {queueId}");
            return new BatchExtensionException($"Updating error status for queue {queueId}");
        }

        return Possible.Completed; 
        
    }

    private async Task<Either<Guid, BatchExtensionException>> CchCreateBatch(ICchService service, LaunchBatchRunRequest request, CancellationToken stoppingToken)
    {
        BatchExtensionException? exception = null;
        Guid batchIdGuid = Guid.Empty;

        List<string> items = request.Returns.Select(r => r.ReturnId).Distinct().ToList();

        var executionId = await service.CreateBatchAsync(request.ReturnType, items, stoppingToken);
        executionId.Match(
            success =>
            {
                batchIdGuid = Guid.TryParse(success, out var parsed) ? parsed : Guid.Empty;
                //batches.AddRange(request.Returns.ConvertToBatchExtensionData(queueId, batchIdGuid));
            },
            failure =>
            {
                //LogErrorMessage(failure.Message);
                exception = new BatchExtensionException($"Error processing batch : {failure.Message}");               
            }
        );
                    
        if (batchIdGuid != Guid.Empty)
            return batchIdGuid;

        if (exception is not null)
            return exception;

        return new BatchExtensionException($"Queue Batches could not be created");
    }

    private async Task<Possible<BatchExtensionException>> AddBatchToDB(IBatchRepository repo, List<BatchExtensionData> batch, CancellationToken stoppingToken)
    {
        var batchResult = await repo.AddBatchAsync(batch, stoppingToken);
        batchResult.WhenFailure(failure =>
        {
            //LogErrorMessage(failure.Message);
            new BatchExtensionException($"Error adding batches to the database : {failure.Message}");
            return;
        });

        return Possible.Completed;

    }

    private async Task<Either<string, BatchExtensionException>> GetBatchStatus(
        ICchService service,
        Guid batchGuidId,
        int batchCount,
        CancellationToken stoppingToken)
    {
        BatchExtensionException? exception = null;
        string status = string.Empty;
        var statusCheck = await service.GetBatchStatusAsync(batchGuidId, batchCount, stoppingToken);
        statusCheck.Match(
            success =>
            {
                if (success.Equals(BatchRecordStatus.Exception.Description))
                {
                    exception = new BatchExtensionException($"Error getting status for batch {batchGuidId}");
                }
                status = success;
            },
            failure =>
            {
                exception = new BatchExtensionException($"Error getting status for batch {batchGuidId} : {failure.Message}");
            }
        );
        if (!status.Equals(string.Empty))
            return status;

        if (exception is not null)
            return exception;

        return new BatchExtensionException($"Could not get status for batch {batchGuidId}");
    }

    private async Task<Either<List<BatchExtensionData>, BatchExtensionException>> CreateCCHBatchFiles(
        ICchService service,
        List<BatchExtensionData> batch,
        Guid batchGuidId, CancellationToken stoppingToken)
    {
        BatchExtensionException? exception = null;
        List<BatchExtensionData>? result = null;

        var batchCreatOutputFiles = await service.CreateBatchOutputFilesAsync(batchGuidId, stoppingToken);
        batchCreatOutputFiles.Match(
            success =>
            {
                batch.UpdateBatchItemsGuidAndFileName(success);                
                result = batch;
            }
            ,
            failure =>
            {
                exception = new BatchExtensionException($"Error getting status for batch {batchGuidId} : {failure.Message}");
            }
        );
        if (exception is not null)
            return exception;

        if (result is not null)
            return result;

        return new BatchExtensionException($"Could not create batch files in CCH for batchGuidId {batchGuidId}");

    }

    private async Task<Possible<BatchExtensionException>> UpdateBatchDataToDB(
        IBatchRepository repo,
        List<BatchExtensionData> batch,
        Guid batchGuidId,
        CancellationToken stoppingToken)
    {
        var batchRepositoryResponse = await repo.UpdateBatchAsync(batch, batchGuidId, stoppingToken);
        if (batchRepositoryResponse.HasFailure)
            return new BatchExtensionException($"Error saving batch updates {batchGuidId} : {batchRepositoryResponse}");

        return Possible.Completed;    
    }

    private async Task<Possible<BatchExtensionException>> DownloadCCHFileAndUploadToGFR(
        ICchService cchService,
        IBatchService batchService,
        IGfrService gfrService,
        List<BatchExtensionData> batch,
        CancellationToken stoppingToken)
    {
        var errors = new List<BatchExtensionException>();

        foreach (var batchExtensionDataItem in batch)
        {            
            var downloadResult = await DownloadBatchFileFromCCH(
                cchService,
                batchService,
                batchExtensionDataItem.BatchId,
                batchExtensionDataItem.BatchItemGuid,
                batchExtensionDataItem.FileName,
                stoppingToken);

            if (downloadResult.HasFailure)
            {
                errors.Add(new BatchExtensionException($"Download failed for BatchItemGuid {batchExtensionDataItem.BatchItemGuid} : {batchExtensionDataItem.FileName}"));
                await batchService.UpdateBatchItemDownloadedFromCCHFailed(batchExtensionDataItem.BatchItemGuid);
            }
            else
            {
                await batchService.UpdateBatchItemDownloadedFromCCH(batchExtensionDataItem.BatchItemGuid);
                var uploadResult = await UploadDocumentToGFR(batchExtensionDataItem, batchService, gfrService);
                if (uploadResult.HasFailure)
                {
                    errors.Add(new BatchExtensionException($"upload to GFR failed for BatchItemGuid {batchExtensionDataItem.BatchItemGuid} : {batchExtensionDataItem.FileName}"));
                    await batchService.UpdateBatchItemUploadedToGFRFailed(batchExtensionDataItem.BatchItemGuid);
                }
                else
                {
                    await batchService.UpdateBatchItemUploadedToGFR(batchExtensionDataItem.BatchItemGuid);
                }
            }
        }

        if (errors.Any())
        {
            var combined = string.Join(Environment.NewLine, errors.Select(e => e.Message));
            return new BatchExtensionException($"Errors occurred while processing batch:{Environment.NewLine}{combined}");
        }

        return Possible.Completed;
    }
    
    private async Task<Possible<BatchExtensionException>> DownloadBatchFileFromCCH(
        ICchService ccService,
        IBatchService batchService,
        Guid batchId,
        Guid batchItemGuid,
        string fileName,
        CancellationToken stoppingToken)
    {
        

        var downloadResult = await ccService.DownloadBatchOutputFilesAsync(batchId, batchItemGuid, fileName, stoppingToken);
        if (downloadResult.HasFailure)
            return new BatchExtensionException($"Error downloading document {batchItemGuid} : {fileName}");

        return Possible.Completed; 

    }

    
    private async Task<Possible<BatchExtensionException>> UploadDocumentToGFR(
        BatchExtensionData document,
        IBatchService batchService,
        IGfrService gfrService)
    {
        BatchExtensionException? exception = null;
        var gfrUpload = await gfrService.UploadDocumentToGfr(
            new GfrDocument(document.TaxReturnId, document.FirmFlowId, document.FileName));
        gfrUpload.Match(
            success => batchService.UpdateBatchItemUploadedToGFR(document.BatchItemGuid),
            failure => exception = new BatchExtensionException(failure.Message)
        );

        if (exception is not null)
            return exception;

        return Possible.Completed;

    }

    
    private void LogErrorMessage(string message) => _logger.LogError("{Message}", message);
    private void LogInformationMessage(string message) => _logger.LogInformation("{Message}", message);

}

