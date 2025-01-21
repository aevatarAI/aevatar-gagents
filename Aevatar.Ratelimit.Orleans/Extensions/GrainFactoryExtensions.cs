
using Aevatar.Orleans.RateLimiting.Core.Interfaces;
using Aevatar.Orleans.RateLimiting.Core.Models;
using Aevatar.Orleans.RateLimiting.Core.Models.Holders;
using Aevatar.Ratelimit;
using Microsoft.Extensions.Options;
using Orleans;

namespace Aevatar.Orleans.RateLimiting.Core.Extensions;

public static class GrainFactoryExtensions
{
    public static ILimiterHolder GetRateLimiter<T>(this IGrainFactory factory, string key) where T : IRateLimiterGrain
    {
        ILimiterHolder limiter = typeof(T) switch
        {
            IFixedWindowRateLimiterGrain => factory.GetFixedWindowRateLimiter(key),

            _ => null //throw new ArgumentException("Unknown rate limiter grain type")
        };

        return limiter;
    }

    public static ILimiterHolder? GetRateLimiterByConfig(this IGrainFactory factory, string key, string configurationName, IEnumerable<RateLimiterConfig> configs)
    {
        var name = configurationName.ToLowerInvariant();
        var option = configs.FirstOrDefault(f => f.Name == name);
        if (option is null)
            return null;

        ILimiterHolder? limiter = option.Configuration switch
        {
            FixedWindowRateLimiterOptions options => factory.GetFixedWindowRateLimiter(key, options),

            _ => null //throw new ArgumentException("Unknown rate limiter grain type")
        };

        if (limiter is ILimiterHolderWithConfiguration<FixedWindowRateLimiterOptions> configurableLimiter &&
            option.Configuration is FixedWindowRateLimiterOptions fixedWindowOptions)
        {
            configurableLimiter.Configure(fixedWindowOptions);
        }
        return limiter;
    }

    public static FixedWindowRateLimiterHolder GetFixedWindowRateLimiter(this IGrainFactory factory, string key)
    {
        return new FixedWindowRateLimiterHolder(factory.GetGrain<IFixedWindowRateLimiterGrain>(key), factory);
    }

    public static FixedWindowRateLimiterHolder GetFixedWindowRateLimiter(this IGrainFactory factory, string key, FixedWindowRateLimiterOptions options)
    {
        return new FixedWindowRateLimiterHolder(factory.GetGrain<IFixedWindowRateLimiterGrain>(key), factory, options);
    }
    
}