using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.MicroAI.Options;
using AutoGen.Core;
using AutoGen.SemanticKernel;
using AutoGen.SemanticKernel.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Orleans;
using Orleans.Providers;

namespace Aevatar.GAgents.MicroAI.Grains;

[StorageProvider(ProviderName = "PubSubStore")]
public class ChatAgentGrain : Grain, IChatAgentGrain
{
    private MiddlewareStreamingAgent<SemanticKernelAgent>? _agent;
    private readonly MicroAIOptions _options;
    private readonly ILogger<ChatAgentGrain> _logger;

    public ChatAgentGrain(IOptions<MicroAIOptions> options, ILogger<ChatAgentGrain> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MicroAIMessage?> SendAsync(string message, List<MicroAIMessage>? chatHistory)
    {
        if (_agent != null)
        {
            var history = ConvertMessage(chatHistory);
            var imMessage = await _agent.SendAsync(message, history);
            return new MicroAIMessage("assistant", imMessage.GetContent()!);
        }

        _logger.LogWarning($"[ChatAgentGrain] Agent is not set");
        return null;
    }

    public Task SetAgentAsync(string systemMessage)
    {
        var kernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_options.Model, _options.Endpoint, _options.ApiKey);
        var systemName = this.GetPrimaryKeyString();
        var kernel = kernelBuilder.Build();
        var kernelAgent = new SemanticKernelAgent(
                kernel: kernel,
                name: systemName,
                systemMessage: systemMessage)
            .RegisterMessageConnector();

        _agent = kernelAgent;
        return Task.CompletedTask;
    }

    public Task SetAgentWithTemperature(string systemMessage, float temperature, int? seed = null,
        int? maxTokens = null)
    {
        var kernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_options.Model, _options.Endpoint, _options.ApiKey);
        var systemName = this.GetPrimaryKeyString();
        var kernel = kernelBuilder.Build();
        var kernelAgent = new SemanticKernelAgent(
                kernel: kernel,
                name: systemName,
                systemMessage: systemMessage)
            .RegisterMessageConnector();

        _agent = kernelAgent;
        return Task.CompletedTask;
    }

    private List<IMessage> ConvertMessage(List<MicroAIMessage> listAutoGenMessage)
    {
        var result = new List<IMessage>();
        foreach (var item in listAutoGenMessage)
        {
            result.Add(new TextMessage(GetRole(item.Role), item.Content));
        }

        return result;
    }

    private Role GetRole(string roleName)
    {
        switch (roleName)
        {
            case "user":
                return Role.User;
            case "assistant":
                return Role.Assistant;
            case "system":
                return Role.System;
            case "function":
                return Role.Function;
            default:
                return Role.User;
        }
    }
}