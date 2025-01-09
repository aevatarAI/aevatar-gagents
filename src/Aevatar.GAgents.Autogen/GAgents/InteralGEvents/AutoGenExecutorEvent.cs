using System;
using Aevatar.GAgents.Autogen.Events;
using Orleans;

namespace Aevatar.GAgents.Autogen.Events.InternalEvents;

[GenerateSerializer]
public class AutoGenExecutorEvent : AutoGenInternalEventBase
{
    [Id(0)]public Guid TaskId { get; set; }
    [Id(1)] public TaskExecuteStatus ExecuteStatus { get; set; }
    [Id(2)] public string CurrentCallInfo { get; set; }
    [Id(3)] public string EndContent { get; set; }
}

public enum TaskExecuteStatus
{
    Progressing,
    Break,
    Finish,
}