using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Orleans;

namespace Aevatar.GAgents.MCP.State;

[GenerateSerializer]
public class MCPGAgentState : StateBase
{
    [Id(0)] public Dictionary<string, MCPServerState> ServerStates { get; set; } = new();
    [Id(1)] public Dictionary<string, MCPToolInfo> AvailableTools { get; set; } = new();
    [Id(2)] public List<MCPServerConfig> ServerConfigs { get; set; } = new();
    [Id(3)] public int TotalToolCalls { get; set; } = 0;
    [Id(4)] public DateTime LastToolCallTime { get; set; }
    [Id(5)] public bool EnableToolDiscovery { get; set; } = true;
    [Id(6)] public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}