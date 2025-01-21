namespace Aevatar.Ratelimit;

public class FixedWindowRateLimiterOptions
{
    /// <summary>
    /// Specifies the time window that takes in the requests.
    /// Must be set to a value greater than <see cref="TimeSpan.Zero" /> by the time these options are passed to the constructor of <see cref="FixedWindowRateLimiter"/>.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Specified whether the <see cref="FixedWindowRateLimiter"/> is automatically refresh counters or if someone else
    /// will be calling <see cref="FixedWindowRateLimiter.TryReplenish"/> to refresh counters.
    /// </summary>
    /// <value>
    /// <see langword="true" /> by default.
    /// </value>
    public bool AutoReplenishment { get; set; } = true;

    /// <summary>
    /// Maximum number of permit counters that can be allowed in a window.
    /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="FixedWindowRateLimiter"/>.
    /// </summary>
    public int PermitLimit { get; set; }
    

    /// <summary>
    /// Maximum cumulative permit count of queued acquisition requests.
    /// Must be set to a value >= 0 by the time these options are passed to the constructor of <see cref="FixedWindowRateLimiter"/>.
    /// </summary>
    public int QueueLimit { get; set; }
}