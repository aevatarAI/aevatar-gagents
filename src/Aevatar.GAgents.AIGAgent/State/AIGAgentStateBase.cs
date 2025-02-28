using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.State;

[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    [Id(0)] public string LLM { get; set; } = string.Empty;

    [Id(1)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(2)] public bool IfUpsertKnowledge { get; set; } = false;
    [Id(3)] public string RetrieveSchema { get; set; } = string.Empty;
    [Id(4)] public string RetrieveExample { get; set; } = string.Empty;
}