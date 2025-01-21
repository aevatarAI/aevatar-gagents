

using Aevatar.Ratelimit;

namespace Aevatar.Orleans.RateLimiting.Core.Interfaces;

public interface IFixedWindowRateLimiterGrain : IRateLimiterGrainWithConfiguration<FixedWindowRateLimiterOptions>
{
    ValueTask<bool> TryReplenishAsync();
}