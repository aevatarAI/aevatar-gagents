using System.Net.Http;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Aevatar.Httpclient;
using Aevatar.Orleans.RateLimiting.Server.Grains;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Core.Abstractions;

public interface IHttpRateLimitClientFactory
{
    Task<HttpClient> GeHttpRateLimitClientAsync(string url,FixedWindowRateLimiterOptions fixedWindowRateLimiterOptions);
}

public class HttpRateLimitClientFactory : IHttpRateLimitClientFactory
{
    
    private readonly IClusterClient _clusterClient;

    public HttpRateLimitClientFactory(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    
    public Task<HttpClient> GeHttpRateLimitClientAsync(string url, FixedWindowRateLimiterOptions fixedWindowRateLimiterOptions)
    {
            
        var rateLimiter = 
            _clusterClient.GetGrain<FixedWindowRateLimiterGrain>(url);
        
        _ = rateLimiter.InitConfigure(fixedWindowRateLimiterOptions);

        HttpClient httpClient = new HttpClient(new ClientSideRateLimitedHandler(rateLimiter));

        return Task.FromResult(httpClient);
    }
}
