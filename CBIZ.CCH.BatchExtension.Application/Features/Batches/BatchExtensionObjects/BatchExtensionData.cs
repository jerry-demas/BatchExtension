
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;

public class BatchExtensionData : BatchExtensionDataBase
{   
    public BatchExtensionQueue? Queue { get; init; }  
}
