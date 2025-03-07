using System;
using System.ClientModel;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAI;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public class OpenAIBrain : BrainBase
{
    public OpenAIBrain(IKernelBuilderFactory kernelBuilderFactory, ILogger<OpenAIBrain> logger, IOptions<RagConfig> ragConfig) :
        base(kernelBuilderFactory, logger, ragConfig)
    {
    }

    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.OpenAI;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.OpenAI;

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        OpenAIClientOptions? clientOptions = null;
        if (!llmConfig.Endpoint.IsNullOrWhiteSpace())
        {
            clientOptions = new OpenAIClientOptions() { Endpoint = new Uri(llmConfig.Endpoint) };
        }

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(llmConfig.ApiKey), clientOptions
        );

        kernelBuilder.AddOpenAIChatCompletion(llmConfig.ModelName, openAiClient);

        return Task.CompletedTask;
    }
}