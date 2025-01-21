using System.Threading.Tasks;
using Aevatar.Orleans.RateLimiting.Core.Interfaces;
using Aevatar.Orleans.RateLimiting.Core.Models;
using Aevatar.Orleans.RateLimiting.Core.Models;

namespace Aevatar.Orleans.RateLimiting.Core.Interfaces;

public interface IRateLimiterGrainWithConfiguration<TOption> : IRateLimiterGrain
{
    ValueTask ConfigureAsync(TOption options);
    ValueTask<TOption> GetConfiguration();
    
    Type GetActualTypeOfTGrain();

    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(TOption options);
    Task<RateLimitLeaseMetadata> AcquireAndCheckConfigurationAsync(int permitCount, TOption options);
}