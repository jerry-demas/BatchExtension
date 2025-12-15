using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;

namespace CBIZ.CCH.BatchExtension.Application.Features.Process;

public record BatchProcessContext(
    IBatchRepository batchRepository,
    IBatchService batchService,
    ICchService cchService,
    IGfrService gfrService,
    IEmailService emailService
);

