using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.SemanticKernel.BrainFactory;

public class BrainFactory : IBrainFactory
{
    private readonly ILogger<BrainFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<Tuple<LLMProviderEnum, ModelIdEnum>, Type> LLMProvider =
        new Dictionary<Tuple<LLMProviderEnum, ModelIdEnum>, Type>();

    public BrainFactory(ILogger<BrainFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        InitLLmProvider();
    }

    public IBrain? GetBrain(LLMProviderConfig llmProviderConfig)
    {
        try
        {
            if (LLMProvider.TryGetValue(
                    new Tuple<LLMProviderEnum, ModelIdEnum>(llmProviderConfig.ProviderEnum, llmProviderConfig.ModelIdEnum),
                    out var type))
            {
                var result = ActivatorUtilities.CreateInstance(_serviceProvider, type);
                return result as IBrain;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when constructing the brain.");
            return null;
        }
    }


    private void InitLLmProvider()
    {
        var typeList = GetAllLLM();
        foreach (var type in typeList)
        {
            var key = GetIBrainKey(type);

            LLMProvider.TryAdd(key, type);
        }
    }

    private List<Type> GetAllLLM()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes()
            .Where(t => typeof(IBrain).IsAssignableFrom(t)
                        && t is { IsClass: true, IsAbstract: false })
            .ToList();

        return types;
    }

    private Tuple<LLMProviderEnum, ModelIdEnum> GetIBrainKey(Type brainType)
    {
        var model = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, brainType);
        var actualDto = model as IBrain;
        return new Tuple<LLMProviderEnum, ModelIdEnum>(actualDto!.ProviderEnum, actualDto.ModelIdEnum);
    }
}