using System;
using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class PublishStateLogEvent : AutogenStateLogEvent
{
    [Id(0)] public Guid EventId { get; set; }
    [Id(1)] public string AgentName { get; set; }
    [Id(2)] public string EventName { get; set; }
}