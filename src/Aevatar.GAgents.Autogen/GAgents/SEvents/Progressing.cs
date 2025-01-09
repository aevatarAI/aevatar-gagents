using System;
using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class CallerProgressing:AutogenEventBase
{
    [Id(0)] public Guid EventId { get; set; }
    [Id(1)] public string CurrentCallInfo { get; set; }
}