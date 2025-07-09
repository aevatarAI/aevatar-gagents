using Orleans;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.Twitter.GAgents.DirectAIAgent;

[GenerateSerializer]
public class DirectAIGAgentConfigDto : ConfigurationBase
{
    [Id(0)] public string Instructions { get; set; } = string.Empty;
    [Id(1)] public LLMConfigDto LLMConfig { get; set; } = new();
} 