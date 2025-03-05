using System;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public abstract class AzureAIInferenceBrain : BrainBase
{
    public AzureAIInferenceBrain(
        IKernelBuilderFactory kernelBuilderFactory,
        ILogger<AzureAIInferenceBrain> logger,
        IOptions<RagConfig> ragConfig)
        : base(kernelBuilderFactory, logger, ragConfig)
    {
    }

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        kernelBuilder.AddAzureAIInferenceChatCompletion(
            llmConfig.ModelName,
            llmConfig.ApiKey,
            new Uri(llmConfig.Endpoint));

        return Task.CompletedTask;
    }
}