using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.Autogen.Options;
using AutoGen.Core;
using AutoGen.SemanticKernel;
using AutoGen.SemanticKernel.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Volo.Abp.DependencyInjection;

namespace Aevatar.GAgents.Autogen;

public class SemanticProvider: IChatAgentProvider, ITransientDependency
{
    private readonly ILogger<SemanticProvider> _logger;
    private readonly SemanticOptions _options;

    private readonly Dictionary<string, MiddlewareAgent<MiddlewareStreamingAgent<SemanticKernelAgent>>> _agents = new();
    
    public SemanticProvider(IOptions<SemanticOptions> options,ILogger<SemanticProvider> logger)
    {
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task<IMessage?> SendAsync(string agentName, string message, IEnumerable<IMessage>? chatHistory)
    {
        if (_agents.TryGetValue(agentName, out var middlewareAgent) == true)
        {
            return await middlewareAgent.SendAsync(message, chatHistory);   
        }
        
        _logger.LogWarning($"[ChatAgentProvider] {agentName} not exist");
        return null;
    }

    public void SetAgent(string agentName, string systemMessage, FunctionCallMiddleware middleware)
    {
        var kernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_options.DeploymentName, _options.Endpoint, _options.ApiKey);
        
        var kernel = kernelBuilder.Build();
        var kernelAgent = new SemanticKernelAgent(
                kernel: kernel,
                name: "agentName",
                systemMessage: systemMessage)
            .RegisterMessageConnector()
            .RegisterMiddleware(middleware);
        _agents.Add(agentName, kernelAgent);
    }
}