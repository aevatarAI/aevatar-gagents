using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Aevatar.Ratelimit;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Aevatar.Orleans.RateLimiting.Server.Grains;

[Reentrant]
[GrainType($"Aevatar.${nameof(FixedWindowRateLimiterGrain)}")]
public class FixedWindowRateLimiterGrain : RateLimiterGrain<FixedWindowRateLimiter, FixedWindowRateLimiterOptions>, IFixedWindowRateLimiterGrain
{
    public FixedWindowRateLimiterGrain(ILogger<FixedWindowRateLimiterGrain> logger) : base(logger,RateLimiterConstants.FixedWindowRateLimiterOptions)
    {
    }

    public Task InitConfigure(FixedWindowRateLimiterOptions options)
    {
        if (options != RateLimiterConstants.FixedWindowRateLimiterOptions)
        {
            _ = ConfigureAsync(options);
        }
        return Task.CompletedTask;
    }



    public ValueTask<bool> TryReplenishAsync()
    {
        return ValueTask.FromResult(RateLimiter.TryReplenish());
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(FixedWindowRateLimiterOptions options)
    {
        if (CheckOptions(options))
            await ConfigureAsync(options);

        return await AcquireAsync();
    }

    public async Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, FixedWindowRateLimiterOptions options)
    {
        if (CheckOptions(options))
            await ConfigureAsync(options);

        return await AcquireAsync(permitCount);
    }

    protected override FixedWindowRateLimiter CreateDefaultRateLimiter()
    {
        return new FixedWindowRateLimiter(Options);
    }

    private bool CheckOptions(FixedWindowRateLimiterOptions options)
    {
        return Options.PermitLimit != options.PermitLimit || Options.QueueLimit != options.QueueLimit || Options.QueueProcessingOrder != options.QueueProcessingOrder ||
               Options.Window != options.Window || Options.AutoReplenishment != options.AutoReplenishment;
    }
}