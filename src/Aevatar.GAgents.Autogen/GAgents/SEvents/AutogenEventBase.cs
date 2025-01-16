using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class AutogenEventBase : StateLogEventBase<AutogenEventBase>
{
    [Id(0)] public Guid TaskId { get; set; }
}