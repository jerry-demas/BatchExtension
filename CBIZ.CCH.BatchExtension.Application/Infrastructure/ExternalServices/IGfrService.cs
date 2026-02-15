using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

using Microsoft.EntityFrameworkCore.Update;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;

public interface IGfrService
{
    Task<Possible<BatchExtensionException>> UploadDocumentToGfr(GfrDocument gfrDocument, IBatchService batchService, BatchExtensionData document, bool refreshGfrTicket, CancellationToken cancellationToken = default);
    Task<Possible<BatchExtensionException>> UpdateFirmFlowDueDate( List<BatchExtensionDeliverableData> deliverableData,  IBatchService batchService, BatchExtensionData document, CancellationToken cancellationToken = default); 
    Task<Possible<BatchExtensionException>> UpdateWorkFlowRoute(string firmFlowId, IBatchService batchService, CancellationToken cancellationToken = default);
    
}
