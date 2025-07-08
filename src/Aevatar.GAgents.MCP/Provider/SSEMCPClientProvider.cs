using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.Provider;

/// <summary>
/// SSE (Server-Sent Events) implementation of MCP client provider
/// </summary>
public class SSEMCPClientProvider : IMCPClientProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SSEMCPClientProvider> _logger;
    private readonly Dictionary<string, SSEMCPClient> _clients = new();

    public SSEMCPClientProvider(HttpClient httpClient, ILogger<SSEMCPClientProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IMCPClient> GetOrCreateClientAsync(MCPServerConfig config)
    {
        if (_clients.TryGetValue(config.ServerName, out var existingClient))
        {
            return Task.FromResult<IMCPClient>(existingClient);
        }

        var client = new SSEMCPClient(config, _httpClient, _logger);
        _clients[config.ServerName] = client;
        
        return Task.FromResult<IMCPClient>(client);
    }

    public async Task DisconnectClientAsync(string serverName)
    {
        if (_clients.TryGetValue(serverName, out var client))
        {
            await client.DisconnectAsync();
            _clients.Remove(serverName);
        }
    }

    public Task<bool> IsConnectedAsync(string serverName)
    {
        if (_clients.TryGetValue(serverName, out var client))
        {
            return Task.FromResult(client.IsConnected);
        }
        return Task.FromResult(false);
    }
}

/// <summary>
/// SSE MCP client implementation
/// </summary>
public class SSEMCPClient : IMCPClient
{
    private readonly MCPServerConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private bool _isConnected = false;
    private CancellationTokenSource? _connectionCts;
    private Task? _eventStreamTask;

    public bool IsConnected => _isConnected;

    public event EventHandler<MCPConnectionStatusEventArgs>? ConnectionStatusChanged;

    public SSEMCPClient(MCPServerConfig config, HttpClient httpClient, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            var endpoint = GetEndpointFromConfig(_config);
            _logger.LogInformation("Connecting to SSE MCP server {ServerName} at {Endpoint}", _config.ServerName, endpoint);

            // SSE connections are not fully implemented yet
            // For now, we'll mark as connected for known SSE servers
            _isConnected = true;
            
            _logger.LogInformation("Connected to SSE server {ServerName} (limited functionality)", _config.ServerName);
            
            ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
            {
                ServerName = _config.ServerName,
                IsConnected = true,
                Message = "Connected to SSE server (limited functionality)"
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to SSE MCP server {ServerName}", _config.ServerName);
            
            ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
            {
                ServerName = _config.ServerName,
                IsConnected = false,
                Message = ex.Message
            });
            
            return false;
        }
    }

    private string GetEndpointFromConfig(MCPServerConfig config)
    {
        // Use URL if available (for SSE connections)
        if (!string.IsNullOrEmpty(config.Url))
        {
            return config.Url;
        }
        
        return config.Command;
    }

    public async Task DisconnectAsync()
    {
        _connectionCts?.Cancel();
        if (_eventStreamTask != null)
        {
            try
            {
                await _eventStreamTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }
        _connectionCts?.Dispose();
        _isConnected = false;

        ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
        {
            ServerName = _config.ServerName,
            IsConnected = false,
            Message = "Disconnected"
        });

        _logger.LogInformation("Disconnected from SSE server {ServerName}", _config.ServerName);
    }

    public Task<List<MCPToolInfo>> DiscoverToolsAsync()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to server {_config.ServerName}");
        }

        // Return predefined tools for known SSE servers
        if (_config.ServerName.Contains("zhipu", StringComparison.OrdinalIgnoreCase) &&
            _config.ServerName.Contains("search", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Returning predefined tools for Zhipu web search SSE server");
            
            return Task.FromResult(new List<MCPToolInfo>
            {
                new MCPToolInfo
                {
                    Name = "web_search",
                    Description = "Search the web using Zhipu AI's web search capabilities",
                    Parameters = new Dictionary<string, MCPParameterInfo>
                    {
                        ["query"] = new MCPParameterInfo
                        {
                            Name = "query",
                            Type = "string",
                            Description = "The search query",
                            Required = true
                        }
                    }
                }
            });
        }

        // Return empty list for other SSE servers
        return Task.FromResult(new List<MCPToolInfo>());
    }

    public Task<MCPToolResult> CallToolAsync(string toolName, Dictionary<string, object> arguments)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to server {_config.ServerName}");
        }

        _logger.LogWarning("SSE tool calling not fully implemented for {ToolName}", toolName);
        
        return Task.FromResult(new MCPToolResult
        {
            Success = false,
            ErrorMessage = $"SSE transport is not fully supported yet. Tool '{toolName}' cannot be called."
        });
    }
}