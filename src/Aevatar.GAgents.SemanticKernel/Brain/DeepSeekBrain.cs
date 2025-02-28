using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public class DeepSeekBrain: AzureAIInferenceBrain
{
    private readonly IOptions<AzureAIInferenceConfig> _config;
    
    public DeepSeekBrain(IOptions<AzureAIInferenceConfig> config, IKernelBuilderFactory kernelBuilderFactory, ILogger<AzureAIInferenceBrain> logger, IOptions<RagConfig> ragConfig) : base(config, kernelBuilderFactory, logger, ragConfig)
    {
        
    }
}