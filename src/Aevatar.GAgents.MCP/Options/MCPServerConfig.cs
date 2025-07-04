using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.MCP.Options;

[GenerateSerializer]
public class MCPServerConfig
{
    [Id(0)] public string ServerName { get; set; } = string.Empty;
    [Id(1)] public string Command { get; set; } = string.Empty;
    [Id(2)] public List<string> Args { get; set; } = new();
    [Id(3)] public Dictionary<string, string> Environment { get; set; } = new();
    [Id(4)] public bool AutoReconnect { get; set; } = true;
    [Id(5)] public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
}