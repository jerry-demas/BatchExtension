

using System.Threading.Channels;

using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

namespace CBIZ.CCH.BatchExtension.Presentation.BackgroundService;


public class BatchQueue
{
    private readonly Channel<BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>> _channel =
        Channel.CreateUnbounded<BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>>();

    public ChannelWriter<BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>> Writer => _channel.Writer;
    public ChannelReader<BatchQueueItem<LaunchBatchQueueRequest, LaunchBatchQueueResponse>> Reader => _channel.Reader;

}


public class BatchQueueItem<TRequest, TResponse>
{
    public TRequest Request { get; }
    public TaskCompletionSource<TResponse> Tcs { get; }

    public BatchQueueItem(TRequest request)
    {
        Request = request;
        Tcs = new TaskCompletionSource<TResponse>();
    }
}