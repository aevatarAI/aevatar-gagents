using System;
using Aevatar.GAgents.Autogen.Common;
using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class CallAgentReply:AutogenEventBase
{
    [Id(0)] public string AgentName { get; set; }
    [Id(1)] public Guid  EventId { get; set; }
    [Id(2)] public AutogenMessage Reply { get; set; }
}