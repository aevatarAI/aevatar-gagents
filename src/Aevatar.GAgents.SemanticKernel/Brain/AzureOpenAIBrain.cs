using System;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public class AzureOpenAIBrain : BrainBase
{
    private readonly IOptions<AzureOpenAIConfig> _azureOpenAIConfig;
    private readonly AzureOpenAIClient _azureOpenAIClient;

    public AzureOpenAIBrain(
        IOptions<AzureOpenAIConfig> azureOpenAIConfig,
        IKernelBuilderFactory kernelBuilderFactory,
        IServiceProvider serviceProvider,
        ILogger<AzureOpenAIBrain> logger,
        IOptions<RagConfig> ragConfig)
        : base(kernelBuilderFactory, logger, ragConfig)
    {
        _azureOpenAIConfig = azureOpenAIConfig;
        _azureOpenAIClient = serviceProvider.GetRequiredKeyedService<AzureOpenAIClient>(AzureOpenAIConfig.ConfigSectionName);
    }

    protected override Task ConfigureKernelBuilder(IKernelBuilder kernelBuilder)
    {
        kernelBuilder.AddAzureOpenAIChatCompletion(
            _azureOpenAIConfig.Value.ChatDeploymentName,
            _azureOpenAIClient);
            
        return Task.CompletedTask;
    }
}