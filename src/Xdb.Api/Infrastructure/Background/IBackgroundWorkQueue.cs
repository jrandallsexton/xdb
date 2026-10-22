using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Background;

/// <summary>
/// Fire-and-forget work that must not delay the response, as the original did with ThreadPool.QueueUserWorkItem.
/// Each item runs in its own DI scope after the request's scope may be gone. Failures are logged, never thrown.
/// Not durable: queued work is lost if the process stops (durable jobs are a later phase).
/// </summary>
public interface IBackgroundWorkQueue
{
    void Enqueue(string description, Func<IServiceProvider, CancellationToken, Task> work);
}
