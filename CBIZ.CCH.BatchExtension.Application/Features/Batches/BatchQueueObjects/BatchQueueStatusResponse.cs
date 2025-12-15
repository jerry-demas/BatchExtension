
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

public record BatchQueueStatusResponse(
    Guid QueueId,    
    string SubmittedBy,
    DateTime SubmittedDate,
    string QueueStatus,
    string ReturnType,   
    List<BatchExtensionData> BatchItems
);

