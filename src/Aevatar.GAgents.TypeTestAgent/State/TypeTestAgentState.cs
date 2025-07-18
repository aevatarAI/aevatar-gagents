using Aevatar.GAgents.AIGAgent.State;
using Orleans;

namespace Aevatar.GAgents.TypeTestAgent.State;

[GenerateSerializer]
public class TypeTestAgentState : AIGAgentStateBase
{
    [Id(0)] public string AgentName { get; set; }
    [Id(1)] public DateTime LastConfigUpdate { get; set; }
    [Id(2)] public bool IsInitialized { get; set; }
} 