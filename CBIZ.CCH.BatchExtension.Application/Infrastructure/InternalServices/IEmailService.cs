using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;

public interface IEmailService
{
    Task<Possible<BatchExtensionException>> SendSuccessEmailAsync(string message,CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> SendFalureEmailAsync(string message,CancellationToken cancellationToken = default);
}
