using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Core.Options;

/// <summary>
/// MCP Server configuration options
/// </summary>
public class MCPServerOptions
{
    /// <summary>
    /// Dictionary of MCP server configurations, keyed by server name
    /// </summary>
    public Dictionary<string, MCPServerConfig> MCPServers { get; set; } = new();

    public bool EnableAllMCPServers { get; set; } = false;
}