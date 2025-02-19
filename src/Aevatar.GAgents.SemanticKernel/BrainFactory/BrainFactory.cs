using System;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.SemanticKernel.BrainFactory;

public class BrainFactory : IBrainFactory
{
    private readonly ILogger<BrainFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BrainFactory(ILogger<BrainFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public IBrain? GetBrain(string llm)
    {
        try
        {
            return _serviceProvider.GetRequiredKeyedService<IBrain>(llm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when constructing the brain.");
            return null;
        }
    }
}