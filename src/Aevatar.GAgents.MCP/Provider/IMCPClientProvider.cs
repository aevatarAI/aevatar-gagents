using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Provider;

public interface IMCPClientProvider
{
    Task<IMCPClient> GetOrCreateClientAsync(MCPServerConfig config);
    Task DisconnectClientAsync(string serverName);
    Task<bool> IsConnectedAsync(string serverName);
}

public interface IMCPClient
{
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<List<MCPToolInfo>> DiscoverToolsAsync();
    Task<MCPToolResult> CallToolAsync(string toolName, Dictionary<string, object> arguments);
    event EventHandler<MCPConnectionStatusEventArgs>? ConnectionStatusChanged;
}

public class MCPConnectionStatusEventArgs : EventArgs
{
    public string ServerName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
}