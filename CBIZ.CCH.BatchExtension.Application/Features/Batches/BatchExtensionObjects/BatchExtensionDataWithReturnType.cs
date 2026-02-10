using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;

public class BatchExtensionDataWithReturnType : BatchExtensionDataBase
{
    public string ReturnType { get; set; } = string.Empty;
    public BatchExtensionQueue? Queue { get; init; }    
}

