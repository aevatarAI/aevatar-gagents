using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.MCP.GEvents;

[GenerateSerializer]
[Description("Discover available tools from MCP server")]
public class MCPDiscoverToolsEvent : EventWithResponseBase<MCPToolsDiscoveredEvent>
{
    [Id(0)] public string ServerName { get; set; } = string.Empty;
}
