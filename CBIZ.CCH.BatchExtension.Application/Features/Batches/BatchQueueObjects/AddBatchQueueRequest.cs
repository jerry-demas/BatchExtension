namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

public record AddBatchQueueRequest(string request)
{
    public AddBatchQueueRequest() : this("") { }
}
