using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.ConfigValidateGagent.GAgents.ConfigValidateGAgent;

[GenerateSerializer]
public class ConfigValidateGAgentState : StateBase
{
    [Id(0)]
    public string Id { get; set; } = string.Empty;
    
    [Id(1)]
    public DateTime LastConfigurationUpdate { get; set; } = DateTime.UtcNow;
}