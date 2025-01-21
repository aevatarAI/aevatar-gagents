using Aevatar.Orleans.RateLimiting.Core.Interfaces;
using Aevatar.Ratelimit;
using Orleans;

namespace Aevatar.Orleans.RateLimiting.Core.Models.Holders;

public class FixedWindowRateLimiterHolder : BaseRateLimiterHolder<IFixedWindowRateLimiterGrain, FixedWindowRateLimiterOptions>
{
    public FixedWindowRateLimiterHolder(IFixedWindowRateLimiterGrain grain, IGrainFactory grainFactory) : base(grain, grainFactory)
    {
    }

    public FixedWindowRateLimiterHolder(IFixedWindowRateLimiterGrain grain, IGrainFactory grainFactory, FixedWindowRateLimiterOptions options) : base(grain, grainFactory,
        options)
    {
    }
}