using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.TypeTestAgent.Events;

[GenerateSerializer]
public class TypeTestStateLogEvent : StateLogEventBase<TypeTestStateLogEvent>
{
    [Id(0)] public string EventType { get; set; }
    [Id(1)] public string Description { get; set; }
    [Id(2)] public DateTime Timestamp { get; set; }
} 