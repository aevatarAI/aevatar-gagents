using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Aevatar.Orleans.RateLimiting.Server.Grains;

public abstract class RateLimiterGrain<TLimiter, TOptions> : Grain where TLimiter : RateLimiter
{
    protected readonly ILogger _logger;
    private readonly ConcurrentDictionary<Guid, RateLimitLease> _rateLimitLeases = new();
    protected TOptions Options;

    protected RateLimiterGrain(ILogger logger, TOptions options)
    {
        _logger = logger;
        Options = options;
        // ReSharper disable once VirtualMemberCallInConstructor
        RateLimiter = CreateDefaultRateLimiter();
    }

    protected TLimiter RateLimiter { get; set; }

    protected abstract TLimiter CreateDefaultRateLimiter();

    public async Task<RateLimitLeaseMetadata> AcquireAsync(int permitCount = 1)
    {

        var lease = await Task.Run(async () => await RateLimiter.AcquireAsync(permitCount));
        
        var guid = Guid.NewGuid();
        _rateLimitLeases.TryAdd(Guid.NewGuid(), lease);

        var orleansLease = new RateLimitLeaseMetadata(guid, this.GetGrainId(), lease);
        return orleansLease;
    }

    public async ValueTask ReleaseLease(Guid guid)
    {
        await Task.Run(() =>
        {
            _rateLimitLeases.Remove(guid, out var lease);
            lease?.Dispose();
        });
    }

    public ValueTask<RateLimiterStatistics?> GetStatisticsAsync()
    {
        return ValueTask.FromResult(RateLimiter.GetStatistics());
    }

    
    public ValueTask ConfigureAsync(TOptions options)
    {
        Options = options;
        RateLimiter = CreateDefaultRateLimiter();
        _logger.LogInformation($"Configured {nameof(SlidingWindowRateLimiter)} with id:{this.GetPrimaryKeyString()}");
        return ValueTask.CompletedTask;
    }

    public ValueTask<TOptions> GetConfiguration()
    {
        return ValueTask.FromResult(Options);
    }
}