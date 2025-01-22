using System;
using System.Threading.RateLimiting;

namespace Aevatar.Ratelimit;

// Static class for global rate limiter configuration
public static class RateLimiterConstants
{
    // Compile-time constants
    private const int PermitLimit = int.MaxValue;               // Maximum permit limit
    private const int QueueLimit = 1;                // Maximum queue limit
    private const int WindowInSeconds = 1;                     // Time window in seconds

    // Runtime static readonly object
    public static readonly FixedWindowRateLimiterOptions FixedWindowRateLimiterOptions = new FixedWindowRateLimiterOptions
    {
        PermitLimit = PermitLimit,                             // Use the constant for permit limit
        QueueLimit = QueueLimit,                               // Use the constant for queue limit
        Window = TimeSpan.FromSeconds(WindowInSeconds),         // Use the constant for time window
        AutoReplenishment = false                              // Disable automatic replenishment
    };
    
    public static readonly TokenBucketRateLimiterOptions TokenBucketRateLimiterOptions = new TokenBucketRateLimiterOptions
    { 
        TokenLimit = PermitLimit, 
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = QueueLimit, 
        ReplenishmentPeriod = TimeSpan.FromMilliseconds(1), 
        TokensPerPeriod = 1, 
        AutoReplenishment = false
    };
}