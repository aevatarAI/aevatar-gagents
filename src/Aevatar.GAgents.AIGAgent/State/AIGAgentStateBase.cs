using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.State;

[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public LLMConfig? LLM { get; set; }

    [Id(1)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(2)] public bool IfUpsertKnowledge { get; set; } = false;
    [Id(3)] public int InputTokenUsage { get; set; } = 0;
    [Id(4)] public int OutTokenUsage { get; set; } = 0;
    [Id(5)] public int TotalTokenUsage { get; set; } = 0;
    [Id(7)] public string RetrieveSchema { get; set; } = string.Empty;
    [Id(8)] public string RetrieveExample { get; set; } = string.Empty;
}