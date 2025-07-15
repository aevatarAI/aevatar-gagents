using Orleans;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Twitter.GAgents.DirectAIAgent;

[GenerateSerializer]
public class DirectAIGAgentConfigDto : ConfigurationBase
{
    [Id(0)] public string Instructions { get; set; } = string.Empty;
    
    [Id(1)] public string SystemLLM { get; set; }
} 