using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;

public interface IGfrService
{
    Task<Possible<BatchExtensionException>> UploadDocumentToGfr(GfrDocument gfrDocument, CancellationToken cancellationToken = default);
}
