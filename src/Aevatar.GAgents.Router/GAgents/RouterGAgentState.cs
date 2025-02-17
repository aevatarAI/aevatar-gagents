using Aevatar.GAgents.Basic.State;
using Aevatar.GAgents.Router.GAgents.Features.Dto;

namespace Aevatar.GAgents.Router.GAgents;

[GenerateSerializer]
public class RouterGAgentState : AIGAgentStateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Dictionary<Type, List<Type>> AgentsInGroup { get; set; } 
    [Id(2)] public List<RouterOutputSchema> RouterHistory { get; set; }
    [Id(3)] public Dictionary<string, AgentDescriptionInfo> AgentDescription =
        new Dictionary<string, AgentDescriptionInfo>();
}