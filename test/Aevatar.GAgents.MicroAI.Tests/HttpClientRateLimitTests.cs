using System.Diagnostics;
using System.Threading.RateLimiting;
using Aevatar.GAgent.MicroAI.Tests;
using Aevatar.Httpclient;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using Aevatar.Orleans.RateLimiting.Server.Grains;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.MicroAi.Tests;

public class HttpClientRateLimitTests : GAgentTestKitBase
{
    private readonly ITestOutputHelper _outputHelper;

    public HttpClientRateLimitTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }


    [Fact]
    public async Task HttpClientRateLimit_Success_Test()
    {
        string exampleUrl = "https://example.com";
        var rateLimiter = await
            Silo.CreateGrainAsync<FixedWindowRateLimiterGrain>(exampleUrl);

        var permit = 10;
        var queueLimit = permit * 3;
        _ = rateLimiter.InitConfigure(new FixedWindowRateLimiterOptions()
        {
            Window = TimeSpan.FromSeconds(1),
            PermitLimit = permit,
            AutoReplenishment = true,
            QueueLimit = queueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
        

        HttpClient httpClient = new HttpClient(new ClientSideRateLimitedHandler(rateLimiter));

        // Create 100 urls with a unique query string.
        var oneHundredUrls = Enumerable.Range(0, 100).Select(
            i => $"https://example.com?iteration={i:0#}");

        var floodOneThroughFortyNineTask = Parallel.ForEachAsync(
            source: oneHundredUrls,
            body: (url, cancellationToken) => GetAsync(httpClient, url, cancellationToken));
        

        await Task.WhenAll(
            floodOneThroughFortyNineTask);

        var statistics = await rateLimiter.GetStatisticsAsync();
        statistics.ShouldNotBeNull();
        _outputHelper.WriteLine("TotalSuccessfulLeases " + statistics.TotalSuccessfulLeases);
        _outputHelper.WriteLine("TotalFailedLeases " + statistics.TotalFailedLeases);
        _outputHelper.WriteLine("CurrentAvailablePermits " + statistics.CurrentAvailablePermits);
        _outputHelper.WriteLine("CurrentQueuedCount " + statistics.CurrentQueuedCount);
        
        statistics.TotalSuccessfulLeases.ShouldBe(queueLimit);
    }
    
    [Fact]
    public async Task HttpClientRateLimit_Fail_Test()
    {
        string exampleUrl = "https://example.com";
        var rateLimiter = await
            Silo.CreateGrainAsync<TokenBucketRateLimiterGrain>(exampleUrl);
        
        
        _ = rateLimiter.InitConfigure(new TokenBucketRateLimiterOptions
        { 
            TokenLimit = 8, 
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 1, 
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(1), 
            TokensPerPeriod = 2, 
            AutoReplenishment = true
        });

        HttpClient httpClient = new HttpClient(new ClientSideRateLimitedHandler(rateLimiter));

        // Create 100 urls with a unique query string.
        var oneHundredUrls = Enumerable.Range(0, 100).Select(
            i => $"https://example.com?iteration={i:0#}");

        var floodOneThroughFortyNineTask = Parallel.ForEachAsync(
            source: oneHundredUrls,
            body: (url, cancellationToken) => GetAsync(httpClient, url, cancellationToken));
        

        await Task.WhenAll(
            floodOneThroughFortyNineTask);

        var statistics = await rateLimiter.GetStatisticsAsync();
        statistics.ShouldNotBeNull();
        _outputHelper.WriteLine("TotalSuccessfulLeases " + statistics.TotalSuccessfulLeases);
        _outputHelper.WriteLine("TotalFailedLeases " + statistics.TotalFailedLeases);
        _outputHelper.WriteLine("CurrentAvailablePermits " + statistics.CurrentAvailablePermits);
        _outputHelper.WriteLine("CurrentQueuedCount " + statistics.CurrentQueuedCount);
        
        statistics.TotalSuccessfulLeases.ShouldBe(100);
    }

    private async ValueTask GetAsync(
        HttpClient client, string url, CancellationToken cancellationToken)
    {
        using var response =
            await client.GetAsync(url, cancellationToken);
        _outputHelper.WriteLine(
            $"URL: {url}, HTTP status code: {response.StatusCode} ({(int)response.StatusCode})");
    }
}