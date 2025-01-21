using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.RateLimiting;
using Aevatar.Interceptor;
using Aevatar.Orleans.RateLimiting.Core.Extensions;
using Aevatar.Orleans.RateLimiting.Core.Models;
using Aevatar.Orleans.RateLimiting.Core.Models.Holders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AISmart.Interceptor;

public class RateLimitFilter : IIncomingGrainCallFilter
{
    private readonly ILogger<RateLimitFilter> _logger;
    private readonly IClusterClient _clusterClient;

    
    private readonly Dictionary<string, RateLimiterConfig> _rateLimiterConfigs;
    
    private readonly Dictionary<string, ILimiterHolder> _rateLimiterDictionnary;

    public RateLimitFilter(
        ILogger<RateLimitFilter> logger,
        IConfiguration configuration,
        IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;


        // Load the rate limiter configurations during initialization
        var loader = new RateLimiterLoader(configuration);
        _rateLimiterConfigs = loader.LoadRateLimiterConfigs();

        if (_rateLimiterConfigs.Count == 0)
        {
            _logger.LogWarning("No rate limiter configurations were found during initialization.");
        }
        else
        {
            _logger.LogInformation("Rate limiter configurations loaded successfully.");
        }
        
        InitializeRateLimiterDictionary();
        
    }
    
    
    private void InitializeRateLimiterDictionary()
    {
        foreach (var config in _rateLimiterConfigs)
        {
            string key = config.Key; 
            RateLimiterConfig rateLimiterConfig = config.Value;

            var rateLimiterHolder = _clusterClient.GetRateLimiterByConfig(key,rateLimiterConfig.GetType().FullName, _rateLimiterConfigs.Values.AsEnumerable());

            _rateLimiterDictionnary[key] = rateLimiterHolder;
        }
    }
    
    
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        _logger.LogDebug("Invoking Grain call: {GrainType}.{MethodName}",
            context.Grain?.GetType().Name,
            context.InterfaceMethod?.Name);

        // Check if the invoked Grain implements the IChatAgentGrain interface
        string grainFullName = context.Grain!.GetType().FullName;
        if (_rateLimiterDictionnary.Keys
            .Any(interfaceType => interfaceType == grainFullName))
        {
            
            _logger.LogInformation("Grain {GrainType} implements IChatAgentGrain. Applying rate limiting.", context.Grain.GetType().Name);
            var rateLimiterHolder = _clusterClient.GetFixedWindowRateLimiter(grainFullName);

            // Check if the RateLimiter allows the request
            var lease = await rateLimiterHolder.AcquireAsync(); // Attempt acquisition
            
            _logger.LogInformation("Rate limit statistics  {GrainType} : {statistics}", 
                grainFullName,
                JsonSerializer.Serialize(await rateLimiterHolder.GetStatisticsAsync()));
            if (!lease.IsAcquired)
            {
                _logger.LogInformation("Rate limit exceeded for Grain: {GrainType}.{MethodName}", 
                    context.Grain?.GetType().Name,
                    context.InterfaceMethod?.Name);
                
                throw new RateLimitExceededException("Rate limit exceeded!");
            }

            try
            {
                _logger.LogDebug("Rate limit acquired successfully for Grain: {GrainType}.{MethodName}", 
                    context.Grain?.GetType().Name,
                    context.InterfaceMethod?.Name);

                // Continue with the Grain method execution
                await context.Invoke();
            }
            finally
            {
                // Ensure resources are released
                lease.Dispose();
                _logger.LogDebug("Rate limit lease disposed for Grain: {GrainType}.{MethodName}", 
                    context.Grain?.GetType().Name,
                    context.InterfaceMethod?.Name);
            }

            return; // Exit early to prevent duplicate invocation
        }

        // Log that no rate limiting is being applied and proceed
        _logger.LogDebug("Grain {GrainType} does not implement IChatAgentGrain. No rate limiting applied.", 
            context.Grain.GetType().Name);

        // Proceed to the actual Grain method invocation
        await context.Invoke();
    }
}

public class RateLimiterLoader
{
    private readonly IConfiguration _configuration;

    public RateLimiterLoader(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Load all rate-limiting configurations
    public Dictionary<string, RateLimiterConfig> LoadRateLimiterConfigs()
    {
        var rateLimiterConfigs = new Dictionary<string, RateLimiterConfig>();

        try
        {
            // Assuming the configuration is under a section called "RateLimiting"
            var rateLimitingSection = _configuration.GetSection("RateLimiting");

            if (rateLimitingSection.Exists())
            {
                foreach (var childSection in rateLimitingSection.GetChildren())
                {
                    // Each rate limiter key, e.g., "Aevatar.GAgents.MicroAI.GAgent.ChatAgentGrain"
                    var key = childSection.Key;

                    // Parse configuration
                    // var config = new RateLimiterConfig();
                    //
                    //
                    // rateLimiterConfigs.Add(key, config);
                }
            }
            else
            {
                Console.WriteLine("No rate-limiting configuration found in settings.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading rate limiter configs: {ex.Message}");
        }

        return rateLimiterConfigs;
    }
}