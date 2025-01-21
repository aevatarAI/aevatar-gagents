using Aevatar.Orleans.RateLimiting.Core.Interfaces;
using Aevatar.Orleans.RateLimiting.Core.Models;
using Aevatar.Ratelimit;
using Aevatar.Ratelimt.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;



namespace Aevatar.Orleans.RateLimiting.Server.Grains;

[Reentrant]
[GrainType($"Aevatar.${nameof(FixedWindowRateLimiterGrain)}")]
public class FixedWindowRateLimiterGrain : RateLimiterGrain<FixedWindowRateQueueLimiter, FixedWindowRateLimiterOptions>, IFixedWindowRateLimiterGrain
{
    public FixedWindowRateLimiterGrain(ILogger<FixedWindowRateLimiterGrain> logger, IOptions<FixedWindowRateLimiterOptions> options) : base(logger, options.Value)
    {
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

    protected override FixedWindowRateQueueLimiter CreateDefaultRateLimiter()
    {
        return new FixedWindowRateQueueLimiter(Options);
    }

    private bool CheckOptions(FixedWindowRateLimiterOptions options)
    {
        return Options.PermitLimit != options.PermitLimit || Options.QueueLimit != options.QueueLimit ||
               Options.Window != options.Window || Options.AutoReplenishment != options.AutoReplenishment;
    }

    public ValueTask ConfigureAsync(FixedWindowRateLimiterOptions options)
    {
        throw new NotImplementedException();
    }

    public ValueTask<FixedWindowRateLimiterOptions> GetConfiguration()
    {
        throw new NotImplementedException();
    }

    public Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(System.Threading.RateLimiting.FixedWindowRateLimiterOptions options)
    {
        throw new NotImplementedException();
    }

    public Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, System.Threading.RateLimiting.FixedWindowRateLimiterOptions options)
    {
        throw new NotImplementedException();
    }
}