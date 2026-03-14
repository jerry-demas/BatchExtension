namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

public static class BatchQueueStatus
{
    public const string Scheduled = "Scheduled";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string CompletedWithErrors = "CompletedWithErrors";
    public const string ReQueued = "ReQueued";
    public const string Cancelled = "Cancelled";
}
