using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public class GeminiBrain : BrainBase
{
    private readonly IOptions<GeminiConfig> _geminiConfig;

    public GeminiBrain(
        IOptions<GeminiConfig> geminiConfig,
        IKernelBuilderFactory kernelBuilderFactory,
        ILogger<AzureOpenAIBrain> logger,
        IOptions<RagConfig> ragConfig)
        : base(kernelBuilderFactory, logger, ragConfig)
    {
        _geminiConfig = geminiConfig;
    }

    protected override Task ConfigureKernelBuilder(IKernelBuilder kernelBuilder)
    {
        kernelBuilder.AddGoogleAIGeminiChatCompletion(
            modelId: _geminiConfig.Value.ModelId,
            apiKey: _geminiConfig.Value.ApiKey);
            
        return Task.CompletedTask;
    }
}