using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.ChatAgent.Dtos;

[GenerateSerializer]
public class ChatConfigDto : ConfigurationBase
{
    [Id(0)]
    [DefaultValues(
        "You are a helpful AI assistant"
    )]
    public string Instructions { get; set; }

    [Id(1)] public LLMConfigDto LLMConfig { get; set; }

    [Id(2)]
    [DefaultValues(20, 10, 50, 100)]
    public int MaxHistoryCount { get; set; } = 20;

    [Id(3)] [DefaultValues(true)] public bool StreamingModeEnabled { get; set; }

    [Id(4)] public StreamingConfig StreamingConfig { get; set; }
}