using System.Threading.RateLimiting;

namespace Aevatar.Orleans.RateLimiting.Core.Models.Holders;

public interface ILimiterHolder
{
    Task<OrleansRateLimitLease> AcquireAsync(int permitCount = 1);
    Task<OrleansRateLimitLease> AcquireAndConfigureAsync(int permitCount = 1);
    ValueTask<RateLimiterStatistics?> GetStatisticsAsync();
}