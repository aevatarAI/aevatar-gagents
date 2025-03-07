using System;
using System.ClientModel;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public class DeepSeekBrain : BrainBase
{
    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.DeepSeek;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.DeepSeek;

    public DeepSeekBrain(IKernelBuilderFactory kernelBuilderFactory, ILogger<DeepSeekBrain> logger, IOptions<RagConfig> ragConfig) :
        base(kernelBuilderFactory, logger, ragConfig)
    {
    }

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(llmConfig.ApiKey),
            new OpenAIClientOptions() { Endpoint = new Uri(llmConfig.Endpoint) }
        );

        kernelBuilder.AddOpenAIChatCompletion(llmConfig.ModelName, openAiClient);

        return Task.CompletedTask;
    }
}