using System;
using Orleans;

namespace AevatarGAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class PublishEvent : AutogenEventBase
{
    [Id(0)] public Guid EventId { get; set; }
    [Id(1)] public string AgentName { get; set; }
    [Id(2)] public string EventName { get; set; }
}