using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;

[GenerateSerializer]
public class MultiAIChatConfig : ConfigurationBase
{
    [Id(0)] public string Instructions { get; set; }
    [Id(1)] public int MaxHistoryCount { get; set; } = 20;
    [Id(2)] public bool StreamingModeEnabled { get; set; }
    [Id(3)] public StreamingConfig StreamingConfig { get; set; }
    [Id(4)] public List<LLMConfigDto> LLMConfigs { get; set; }
    [Id(5)] public TimeSpan RequestRecoveryDelay { get; set; } = TimeSpan.FromMinutes(1);
}