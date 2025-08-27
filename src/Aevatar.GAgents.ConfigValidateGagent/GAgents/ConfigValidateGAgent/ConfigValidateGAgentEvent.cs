using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.ConfigValidateGagent.GAgents.ConfigValidateGAgent;

[GenerateSerializer]
public abstract class ConfigValidateGAgentEvent : StateLogEventBase<ConfigValidateGAgentEvent>;

[GenerateSerializer]
public class ConfigurationUpdatedEvent : ConfigValidateGAgentEvent
{
    [Id(0)] public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
}