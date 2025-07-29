using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.ChatAgent.Dtos;

[GenerateSerializer]
public class ChatConfigDto: ConfigurationBase
{
    [Id(0)]
    public string Instructions { get; set; }
    [Id(1)]
    public LLMConfigDto LLMConfig { get; set; }
    [Id(2)] public int MaxHistoryCount { get; set; } = 20;
    [Id(3)] public bool StreamingModeEnabled { get; set; }
    [Id(4)] public StreamingConfig StreamingConfig { get; set; }
}