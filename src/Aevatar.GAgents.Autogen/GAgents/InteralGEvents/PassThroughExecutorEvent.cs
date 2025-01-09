using System;
using Orleans;

namespace Aevatar.GAgents.Autogen.Events.InternalEvents;

[GenerateSerializer]
public class PassThroughExecutorEvent : AutoGenInternalEventBase
{
    [Id(0)] public Guid TaskId { get; set; }
    [Id(1)] public string AgentName { get; set; }
    [Id(2)] public string EventName { get; set; }
    [Id(3)] public object PassThroughData { get; set; }
}