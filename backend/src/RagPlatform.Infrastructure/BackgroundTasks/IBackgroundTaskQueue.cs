namespace RagPlatform.Infrastructure.BackgroundTasks;

/// <summary>
/// A minimal in-process work queue used to run document ingestion off the
/// request thread. Each work item receives a fresh DI scope.
/// </summary>
public interface IBackgroundTaskQueue
{
    ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem);

    ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(
        CancellationToken cancellationToken);
}
