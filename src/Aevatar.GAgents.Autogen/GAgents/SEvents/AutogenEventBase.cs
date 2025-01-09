using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class AutogenEventBase:GEventBase
{
    [Id(0)] public Guid TaskId { get; set; }
}