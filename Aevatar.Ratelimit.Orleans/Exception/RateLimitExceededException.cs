using System;

namespace Aevatar.Interceptor;


public class RateLimitExceededException : Exception
{
    public RateLimitExceededException() : base("Rate limit exceeded")
    {
        Reason = "Aevatar Rate limit exceeded";
        RetryAfter = TimeSpan.Zero;
    }

    public RateLimitExceededException(string reason) : base(reason)
    {
        Reason = reason;
        RetryAfter = TimeSpan.Zero;
    }

    public RateLimitExceededException(TimeSpan retry) : base("Aevatar Time limit exceeded")
    {
        Reason = "Aevatar Time limit exceeded";
        RetryAfter = retry;
    }

    public RateLimitExceededException(string reason, TimeSpan retry) : base(reason)
    {
        Reason = reason;
        RetryAfter = retry;
    }

    public string Reason { get; set; }
    public TimeSpan RetryAfter { get; set; }
}