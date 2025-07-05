using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.Provider;

/// <summary>
/// Mock implementation of IMCPClientProvider for testing
/// </summary>
public class MockMCPClientProvider : IMCPClientProvider
{
    private readonly Dictionary<string, IMCPClient> _clients = new();
    private readonly ILogger<MockMCPClientProvider> _logger;

    public MockMCPClientProvider(ILogger<MockMCPClientProvider> logger)
    {
        _logger = logger;
    }

    public Task<IMCPClient> GetOrCreateClientAsync(MCPServerConfig config)
    {
        if (!_clients.ContainsKey(config.ServerName))
        {
            _clients[config.ServerName] = new MockMCPClient(config, _logger);
        }
        
        return Task.FromResult(_clients[config.ServerName]);
    }

    public Task DisconnectClientAsync(string serverName)
    {
        if (_clients.ContainsKey(serverName))
        {
            _clients.Remove(serverName);
        }
        
        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync(string serverName)
    {
        if (_clients.ContainsKey(serverName))
        {
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }
}

public class MockMCPClient : IMCPClient
{
    private readonly MCPServerConfig _config;
    private readonly ILogger _logger;
    private bool _isConnected = false;

    public event EventHandler<MCPConnectionStatusEventArgs>? ConnectionStatusChanged;

    public MockMCPClient(MCPServerConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<bool> ConnectAsync()
    {
        _logger.LogInformation("Mock connecting to server {ServerName}", _config.ServerName);
        _isConnected = true;
        
        ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
        {
            ServerName = _config.ServerName,
            IsConnected = true,
            Message = "Mock connected"
        });
        
        return Task.FromResult(true);
    }

    public Task DisconnectAsync()
    {
        _logger.LogInformation("Mock disconnecting from server {ServerName}", _config.ServerName);
        _isConnected = false;
        
        ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
        {
            ServerName = _config.ServerName,
            IsConnected = false,
            Message = "Mock disconnected"
        });
        
        return Task.CompletedTask;
    }

    public Task<List<MCPToolInfo>> DiscoverToolsAsync()
    {
        _logger.LogInformation("Mock discovering tools on server {ServerName}", _config.ServerName);
        
        // Return some mock tools
        var tools = new List<MCPToolInfo>
        {
            new MCPToolInfo
            {
                Name = "read_file",
                Description = "Read file contents",
                Parameters = new Dictionary<string, MCPParameterInfo>
                {
                    ["path"] = new MCPParameterInfo
                    {
                        Name = "path",
                        Type = "string",
                        Description = "File path to read",
                        Required = true
                    }
                }
            },
            new MCPToolInfo
            {
                Name = "list_directory",
                Description = "List directory contents",
                Parameters = new Dictionary<string, MCPParameterInfo>
                {
                    ["path"] = new MCPParameterInfo
                    {
                        Name = "path",
                        Type = "string",
                        Description = "Directory path to list",
                        Required = true
                    }
                }
            }
        };
        
        return Task.FromResult(tools);
    }

    public Task<MCPToolResult> CallToolAsync(string toolName, Dictionary<string, object> arguments)
    {
        _logger.LogInformation("Mock calling tool {ToolName} with args: {Args}", 
            toolName, string.Join(", ", arguments.Keys));
        
        // Return a mock result
        return Task.FromResult(new MCPToolResult
        {
            Success = true,
            Data = $"Mock result for {toolName}",
            Metadata = new Dictionary<string, string>
            {
                ["server"] = _config.ServerName,
                ["tool"] = toolName
            }
        });
    }
}