using Aevatar.GAgents.Router.GAgents.Features.Dto;

namespace Aevatar.GAgents.Router.GAgents.SEvents;

[GenerateSerializer]
public class SetAgentDescriptionsEvent : RouterGAgentSEvent
{
    [Id(0)] public Dictionary<Type, List<Type>> Agents { get; set; }
    [Id(1)] public Dictionary<string, AgentDescriptionInfo> AgentDescriptions { get; set; }
}