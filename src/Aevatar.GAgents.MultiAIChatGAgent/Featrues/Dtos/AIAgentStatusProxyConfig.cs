using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;

[GenerateSerializer]
public class AIAgentStatusProxyConfig : ConfigurationBase
{
    [Id(0)] public string Instructions { get; set; }
    [Id(1)] public LLMConfigDto LLMConfig { get; set; }
    [Id(3)] public bool StreamingModeEnabled { get; set; }
    [Id(4)] public StreamingConfig StreamingConfig { get; set; }
    [Id(5)] public TimeSpan? RequestRecoveryDelay { get; set; }
    [Id(6)] public Guid ParentId { get; set; }
}