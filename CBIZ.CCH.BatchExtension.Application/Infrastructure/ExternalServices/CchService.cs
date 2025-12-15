using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using CBIZ.CCH.BatchExtension.ApplicationFeatures.Batches;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;


using CBIZ.CCH.BatchExtension.Application.Shared;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;

public class CchService : ICchService
{
    private readonly ILogger<CchService> _logger;
    private readonly CchEndPointOptions _cchEndPointsOptions;
    private readonly ProcessOptions _processOptions;   
    private readonly ApiHelper _apiHelper;

    public CchService(
        ILogger<CchService> logger,
        IOptions<CchEndPointOptions> cchEndPointsOptions,
        IOptions<ProcessOptions> processOptions,
        ApiHelper apiHelper

        )
    {
        _logger = logger;
        _cchEndPointsOptions = cchEndPointsOptions.Value;
        _processOptions = processOptions.Value;
        _apiHelper = apiHelper;
    }


    public async Task<Either<string, BatchExtensionException>> CreateBatchAsync(
            string returnType,
            List<string> returnIds,
            CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In CreateBatchAsync");
        try
        {
            if (_processOptions.UseCchMockData) return CchMockData.TestBatchId().ToString();

            var url = $"{ApiURL(_cchEndPointsOptions.CreateBatchAPI)}/{returnType}";
            StringContent content = new StringContent(JsonSerializer.Serialize(returnIds), Encoding.UTF8, "application/json");
            var createResponse = await _apiHelper.ExecutePostAsync(url, content, null, cancellationToken);
            if (createResponse.HasFailure)
            {
                return new BatchExtensionException($"Error in CreateBatchAsync: {createResponse.Failure}");
            }

            var final = await _apiHelper.DeserializeResult<CreateBatchResult>(createResponse.Value);
            if (final.HasFailure)
            {
                return new BatchExtensionException($"Create batch CreateBatchAsync for returns: {returnIds}");
            }

            return final.Value.ExecutionId.ToString();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch for returnIds: {ReturnIds}", string.Join(", ", returnIds));
            return new BatchExtensionException($"Error creating batch for returnIds: {returnIds}", ex);
        }
    }
    
    public async Task<Either<(List<BatchItemStatus> items, string status), BatchExtensionException>> GetBatchStatusAsync(
           Guid executionId,
           int returnsCount,
           CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In GetBatchStatusAsync");
        try
        {
            
            if (_processOptions.UseCchMockData) {
                await Task.Delay(BatchStatusTimeDelay(returnsCount), cancellationToken);
                return CchMockData.TestBatchStatusDescription();
            }
            
            await Task.Delay(BatchStatusTimeDelay(returnsCount), cancellationToken);
            var url = $"{ApiURL(_cchEndPointsOptions.GetBatchStatusAPI)}/{executionId}";
            int retryAttempts = 0;
            while (retryAttempts < _processOptions.StatusRetryLimit)
            {
                var statusResponse = await _apiHelper.ExecuteGetAsync(url, null, cancellationToken);
                if (statusResponse.HasFailure)
                {
                    return new BatchExtensionException($"Error getting status for Execution ID: {executionId}");
                }

                var final = await _apiHelper.DeserializeResult<GetBatchStatusResponse>(statusResponse.Value);
                if (final.HasFailure)
                {
                    return new BatchExtensionException($"Error getting status for Execution ID: {executionId}");
                }
                
                if (final.Value.BatchStatusDescription == BatchRecordStatus.Complete.Description)
                {
                   return (final.Value.items.ToList(), final.Value.BatchStatusDescription); 
                } else if (final.Value.BatchStatusDescription == BatchRecordStatus.Exception.Description)
                {
                    if(BatchHasItemsCompleted(final.Value))
                    {
                        return (final.Value.items.ToList(), BatchRecordStatus.Complete.Description);
                    } else
                    {
                        return (final.Value.items.ToList(), final.Value.BatchStatusDescription); 
                    }
                }

                retryAttempts++;

                await Task.Delay(BatchStatusTimeDelay(returnsCount), cancellationToken);
            }
            return new BatchExtensionException($"Batch did not complete after {_processOptions.StatusRetryLimit} retries.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for Execution ID: {ExecutionId}", executionId);
            return new BatchExtensionException($"Error getting status for Execution ID: {executionId}");
        }
    }
    public async Task<Either<List<CreateBatchOutputFilesResponse>, BatchExtensionException>> CreateBatchOutputFilesAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In CreateBatchOutputFilesAsync");
        try
        {

            if (_processOptions.UseCchMockData) return CchMockData.TestBatchCreateBatchOutputFilesResponse();

            Console.WriteLine(ApiURL(_cchEndPointsOptions.GetBatchOutputFilesAPI));
            var url = $"{ApiURL(_cchEndPointsOptions.GetBatchOutputFilesAPI)}/{executionId}";

            var response = await _apiHelper.ExecuteGetAsync(url, null, cancellationToken);
            if (response.HasFailure)
            {
                return new BatchExtensionException($"Error creating batch output files for Execution ID : {executionId}", response.Failure);
            }

            var final = await _apiHelper.DeserializeResult<List<CreateBatchOutputFilesResponse>>(response.Value);
            if (final.HasFailure)
            {
                return new BatchExtensionException($"Error creating batch output files for Execution ID : {executionId}", response.Failure);
            }

            return final.Value;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch output files for Execution ID : {ExecutionId}", executionId);
            return new BatchExtensionException($"Error creating batch output files for Execution ID : {executionId}");
        }

    }

    public async Task<Possible<BatchExtensionException>> DownloadBatchOutputFilesAsync(
           Guid executionId,
           Guid batchGUID,
           string fileName,
           CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In DownloadBatchOutputFilesAsync");
        try
        {
            
            if (_processOptions.UseCchMockData) return Possible.Completed; 

            var url = $"{ApiURL(_cchEndPointsOptions.BatchOutputDownloadFileAPI)}/{executionId}/{batchGUID}/{fileName}";
            var response = await _apiHelper.ExecuteGetAsync(url, null, cancellationToken);
            if (response.HasFailure)
            {
                return new BatchExtensionException($"Error downloading file from API: {response.Failure}");
            }

            HelperFunctions.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), _processOptions.DownloadFilesDirectory));
            var returnFileName = Path.Combine(Directory.GetCurrentDirectory(), _processOptions.DownloadFilesDirectory, fileName);
            await using (Stream contentStream = await response.Value.Content.ReadAsStreamAsync(cancellationToken),
                fileStream = new FileStream(returnFileName,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                await contentStream.CopyToAsync(fileStream, cancellationToken);
            }
            return Possible.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in DownloadBatchOutputFilesAsync.");
            return new BatchExtensionException($"Error downloading file from API: {ex}");
        }
        
    }

    private string ApiURL(string cchAPI) => String.Concat(_cchEndPointsOptions.Domain, "/", cchAPI);
    private int BatchStatusTimeDelay(int returnCount = 1)
    {
        var milliSeconds = _processOptions.StatusTimeIntervalSeconds * 1000;
        return milliSeconds * returnCount;
    }

    private static bool BatchHasItemsCompleted(GetBatchStatusResponse statusResponse)
    {      
        return statusResponse.items.Any(r => r.ItemStatusCode == BatchItemRecordStatus.Complete.Code);
    }

}
