using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.State;

[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public LLMConfig? LLM { get; set; }
    [Id(1)] public string? SystemLLM { get; set; } = null;
    [Id(2)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(3)] public bool IfUpsertKnowledge { get; set; } = false;
    [Id(4)] public int InputTokenUsage { get; set; } = 0;
    [Id(5)] public int OutTokenUsage { get; set; } = 0;
    [Id(6)] public int TotalTokenUsage { get; set; } = 0;
    [Id(7)] public bool StreamingModeEnabled { get; set; }
    [Id(8)] public StreamingConfig StreamingConfig { get; set; }
    [Id(9)] public int LastInputTokenUsage { get; set; } = 0;
    [Id(10)] public int LastOutTokenUsage { get; set; } = 0;
    [Id(11)] public int LastTotalTokenUsage { get; set; } = 0;
    [Id(12)] public string? LLMConfigKey { get; set; } = null;
}