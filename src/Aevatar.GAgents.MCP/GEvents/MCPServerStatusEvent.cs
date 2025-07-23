using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.MCP.GEvents;

[GenerateSerializer]
public class MCPServerStatusEvent : EventBase
{
    [Id(0)] public string ServerName { get; set; } = string.Empty;
    [Id(1)] public bool IsConnected { get; set; }
    [Id(2)] public string StatusMessage { get; set; } = string.Empty;
}
