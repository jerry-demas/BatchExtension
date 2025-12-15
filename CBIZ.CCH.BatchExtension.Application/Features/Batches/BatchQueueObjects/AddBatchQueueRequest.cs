namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

public record AddBatchQueueRequest(string Request)
{
    public AddBatchQueueRequest() : this("") { }
}
