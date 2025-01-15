using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.MicroAI.GAgent.StateLogEvent;

[GenerateSerializer]
public class AIMessageStateLogEvent:StateLogEventBase<AIMessageStateLogEvent>
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
}
