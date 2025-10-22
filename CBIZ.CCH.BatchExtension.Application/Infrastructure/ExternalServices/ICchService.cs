using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;

public interface ICchService
{
    Task<Either<string , BatchExtensionException>> CreateBatchAsync(string returnType, List<string> returnIds, CancellationToken cancellationToken = default);
    Task<Either<string , BatchExtensionException>> GetBatchStatusAsync(Guid executionId, int returnsCount, CancellationToken cancellationToken = default);
    Task<Either<List<CreateBatchOutputFilesResponse> , BatchExtensionException>>  CreateBatchOutputFilesAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> DownloadBatchOutputFilesAsync(Guid executionId, Guid batchGUID, string fileName, CancellationToken cancellationToken = default);
}
