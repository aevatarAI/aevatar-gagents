using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.MCP.Options;

[GenerateSerializer]
public class MCPGAgentConfig : ConfigurationBase
{
    [Id(0)] public List<MCPServerConfig> Servers { get; set; } = new();
    [Id(1)] public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    [Id(2)] public bool EnableToolDiscovery { get; set; } = true;
}