using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.SyncWork;
using Orleans.SyncWork.Enums;

namespace Aevatar.AI.Feature.StreamSyncWoker;

public abstract class StreamAsyncWorker<TRequest, TResponse> : SyncWorker<TRequest, TResponse>,
    IStreamAsyncWorker<TRequest, TResponse>
{
    private IStreamHandler<TResponse> _streamHandler;
    private GrainId _grainId;
    private readonly ILogger<StreamAsyncWorker<TRequest, TResponse>> _logger;

    public StreamAsyncWorker(ILogger<StreamAsyncWorker<TRequest, TResponse>> logger,
        LimitedConcurrencyLevelTaskScheduler limitedConcurrencyScheduler) : base(logger, limitedConcurrencyScheduler)
    {
        _logger = logger;
    }

    protected override async Task<TResponse> PerformWork(TRequest request,
        GrainCancellationToken grainCancellationToken)
    {
        _logger.LogDebug($"[StreamAsyncWorker] Performing long run task for request of type {typeof(TRequest).FullName}: {request}");
        try
        {
            var response = await PerformLongRunTask(_streamHandler, request);
            await _streamHandler.HandleStreamAsync(response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[StreamAsyncWorker] Error performing long run task: {ex.Message}");
            throw;
        }
    }

    protected abstract Task<TResponse> PerformLongRunTask(IStreamHandler<TResponse> streamHandler, TRequest request);

    public Task SetLongRunTaskAsync(GrainId grainId)
    {
        _grainId = grainId;
        _streamHandler = GrainFactory.GetGrain<IStreamHandler<TResponse>>(grainId);
        return Task.CompletedTask;
    }
}