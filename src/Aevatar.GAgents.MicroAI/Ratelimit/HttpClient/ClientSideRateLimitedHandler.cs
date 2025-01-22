using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;

namespace Aevatar.Httpclient
{
    public class ClientSideRateLimitedHandler : DelegatingHandler, IAsyncDisposable
    {
        private readonly IRateLimiterGrain _limiter;

        public ClientSideRateLimitedHandler(IRateLimiterGrain? limiter) 
            : base(new HttpClientHandler())
        {
            _limiter = limiter ?? throw new ArgumentNullException(nameof(limiter));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Acquire a lease from the rate limiter.
            RateLimitLeaseMetadata lease = await _limiter.AcquireAsync(
                permitCount: 1);

            if (lease.IsAcquired)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // If limit not acquired, return HTTP 429 Too Many Requests.
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            var retryAfterMetadata = lease?.Metadata.FirstOrDefault(kvp => kvp.Key == "Retry-After");


            if (retryAfterMetadata != null)
                response.Headers.Add(
                    "Retry-After",
                    (retryAfterMetadata.Value.Value?.ToString()));

            return response;
        }

        public async ValueTask DisposeAsync()
        { 
            // await _limiter.DisposeAsync().ConfigureAwait(false);

            // await _limiter
            // Dispose base class and suppress finalization.
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                // _limiter.Dispose();
            }
        }
    }
}