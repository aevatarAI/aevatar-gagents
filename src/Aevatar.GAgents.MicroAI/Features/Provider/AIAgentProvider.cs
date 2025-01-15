using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.MicroAI.Agent.SEvents;
using Aevatar.GAgents.MicroAI.GAgent;
using AutoGen.Core;
using AutoGen.SemanticKernel;
using AutoGen.SemanticKernel.Extension;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Volo.Abp.DependencyInjection;

namespace Aevatar.GAgents.MicroAI.Provider;

public class AIAgentProvider : IAIAgentProvider, ISingletonDependency
{
    private readonly MicroAIOptions _options;

    public AIAgentProvider(IOptions<MicroAIOptions> options)
    {
        _options = options.Value;
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

    public async Task<MicroAIMessage?> SendAsync(MiddlewareStreamingAgent<SemanticKernelAgent> agent, string message, List<MicroAIMessage>? chatHistory)
    {
        var history = ConvertMessage(chatHistory);
        var imMessage = await agent.SendAsync(message, history);
        return new MicroAIMessage("assistant", imMessage.GetContent()!);
    }

    public async Task<MiddlewareStreamingAgent<SemanticKernelAgent>> GetAgentAsync(string agentName, string systemMessage)
    {
        var kernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_options.Model, _options.Endpoint, _options.ApiKey);
        var kernel = kernelBuilder.Build();
        var kernelAgent = new SemanticKernelAgent(
                kernel: kernel,
                name: agentName,
                systemMessage: systemMessage)
            .RegisterMessageConnector();

        return kernelAgent;
    }
}