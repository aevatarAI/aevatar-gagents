using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class AutogenEventBase:GEventBase
{
    [Id(0)] public Guid TaskId { get; set; }
}