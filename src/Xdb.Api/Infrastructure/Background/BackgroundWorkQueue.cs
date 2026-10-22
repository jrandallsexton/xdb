using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Xdb.Api.Infrastructure.Background;

public sealed class BackgroundWorkQueue : IBackgroundWorkQueue
{
    private readonly Channel<WorkItem> _channel = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(string description, Func<IServiceProvider, CancellationToken, Task> work)
    {
        if (!_channel.Writer.TryWrite(new WorkItem(description, work)))
            throw new InvalidOperationException("Background work queue is closed.");
    }

    internal ChannelReader<WorkItem> Reader => _channel.Reader;

    internal sealed record WorkItem(string Description, Func<IServiceProvider, CancellationToken, Task> Work);
}

/// <summary>Runs queued background work items one at a time, each in a fresh DI scope.</summary>
public sealed class BackgroundWorkService(BackgroundWorkQueue queue, IServiceScopeFactory scopes, ILogger<BackgroundWorkService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await item.Work(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Background work failed: {Description}", item.Description);
            }
        }
    }
}
