using System;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public sealed class AzureOpenAIBrain : BrainBase
{
    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.Azure;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.OpenAI;

    public AzureOpenAIBrain(
        IKernelBuilderFactory kernelBuilderFactory,
        ILogger<AzureOpenAIBrain> logger,
        IOptions<RagConfig> ragConfig)
        : base(kernelBuilderFactory, logger, ragConfig)
    {
    }

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        var azureOpenAi = new AzureOpenAIClient(
            new Uri(llmConfig.Endpoint),
            new AzureKeyCredential(llmConfig.ApiKey)
        );

        kernelBuilder.AddAzureOpenAIChatCompletion(
            llmConfig.ModelName,
            azureOpenAi);

        return Task.CompletedTask;
    }
}