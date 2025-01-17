using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class AutogenStateLogEvent:StateLogEventBase <AutogenStateLogEvent>
{
    [Id(0)] public Guid TaskId { get; set; }
}