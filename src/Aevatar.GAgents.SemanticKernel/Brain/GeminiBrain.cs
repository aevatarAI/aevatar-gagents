using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public sealed class GeminiBrain : BrainBase
{
    public GeminiBrain(
        IKernelBuilderFactory kernelBuilderFactory,
        ILogger<AzureOpenAIBrain> logger,
        IOptions<RagConfig> ragConfig)
        : base(kernelBuilderFactory, logger, ragConfig)
    {
    }

    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.Google;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.Gemini;

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        kernelBuilder.AddGoogleAIGeminiChatCompletion(
            modelId: llmConfig.ModelName,
            apiKey: llmConfig.ApiKey);

        return Task.CompletedTask;
    }
}