using CBIZ.CCH.BatchExtension.Application.Features.Batches;

namespace CBIZ.CCH.BatchExtension.ApplicationFeatures.Batches;

public record CreateBatchResult(Guid ExecutionId, FileResult[] FileResults);


