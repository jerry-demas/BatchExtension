namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

public record LaunchBatchQueueRequest(Guid QueueId)
{
   public LaunchBatchQueueRequest() : this(Guid.Empty) { } 
}
