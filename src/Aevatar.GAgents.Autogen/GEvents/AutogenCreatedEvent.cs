using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Autogen.Events.Dto;


[GenerateSerializer]
public class AutoGenCreatedEvent: EventBase
{
    [Id(0)] public Guid EventId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// user input
    /// </summary>
    [Id(1)]public string Content { get; set; }
}