using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;

public interface IEmailService
{
    Task<Possible<BatchExtensionException>> SendEmailSuccessAsync(string submittedBy, string message,CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> SendEmailFailureAsync(string submittedBy, string message, CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> SendEmailSuccessfullQueueProcessAsync(string submittedBy,
       List<BatchQueueStatusResponse> status,
       CancellationToken cancellationToken = default);

    Task<Possible<BatchExtensionException>> SendEmailFailedQueueProcessAsync(string submittedBy,
       List<BatchQueueStatusResponse> status,
       CancellationToken cancellationToken = default);

}
