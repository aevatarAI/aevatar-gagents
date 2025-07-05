using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.Provider;

/// <summary>
/// Real implementation of MCP client provider using JSON-RPC over HTTP
/// </summary>
public class RealMCPClientProvider : IMCPClientProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RealMCPClientProvider> _logger;
    private readonly Dictionary<string, RealMCPClient> _clients = new();

    public RealMCPClientProvider(HttpClient httpClient, ILogger<RealMCPClientProvider> logger)
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

        var client = new RealMCPClient(config, _httpClient, _logger);
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
/// Real MCP client implementation using JSON-RPC over HTTP
/// </summary>
public class RealMCPClient : IMCPClient
{
    private readonly MCPServerConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private int _requestId = 0;
    private bool _isConnected = false;
    private ServerInfo? _serverInfo;
    private ServerCapabilities? _serverCapabilities;

    public bool IsConnected => _isConnected;

    public event EventHandler<MCPConnectionStatusEventArgs>? ConnectionStatusChanged;

    public RealMCPClient(MCPServerConfig config, HttpClient httpClient, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            // Build endpoint from command and args
            var endpoint = GetEndpointFromConfig(_config);
            _logger.LogInformation("Connecting to MCP server {ServerName} at {Endpoint}", _config.ServerName, endpoint);

            // Send initialize request
            var initRequest = new JsonRpcRequest
            {
                Method = "initialize",
                Params = new InitializeParams
                {
                    ProtocolVersion = "2025-06-18",
                    ClientInfo = new ClientInfo
                    {
                        Name = "AevatarMCPClient",
                        Version = "1.0.0"
                    },
                    Capabilities = new ClientCapabilities
                    {
                        Roots = new { },
                        Sampling = new { },
                        Elicitation = new { }
                    }
                },
                Id = NextRequestId()
            };

            var response = await SendRequestAsync<InitializeResult>(endpoint, initRequest);
            if (response?.ServerInfo != null)
            {
                _serverInfo = response.ServerInfo;
                _serverCapabilities = response.Capabilities;
                _isConnected = true;

                _logger.LogInformation("Successfully connected to {ServerName}", _config.ServerName);
                    
                ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
                {
                    ServerName = _config.ServerName,
                    IsConnected = true,
                    Message = "Connected successfully"
                });
                    
                return true;
            }

            _logger.LogError("Failed to initialize connection to {ServerName}", _config.ServerName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to MCP server {ServerName}", _config.ServerName);
                
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
        // For HTTP servers, the command should be the URL
        // For stdio servers, this would need different handling
        return config.Command;
    }

    public async Task DisconnectAsync()
    {
        _isConnected = false;
        _serverInfo = null;
        _serverCapabilities = null;
            
        ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
        {
            ServerName = _config.ServerName,
            IsConnected = false,
            Message = "Disconnected"
        });
            
        _logger.LogInformation("Disconnected from {ServerName}", _config.ServerName);
        await Task.CompletedTask;
    }

    public async Task<List<MCPToolInfo>> DiscoverToolsAsync()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to server {_config.ServerName}");
        }

        try
        {
            var request = new JsonRpcRequest
            {
                Method = "tools/list",
                Params = new { },
                Id = NextRequestId()
            };

            var endpoint = GetEndpointFromConfig(_config);
            var response = await SendRequestAsync<ToolsListResult>(endpoint, request);
            if (response?.Tools != null)
            {
                return response.Tools.Select(tool => new MCPToolInfo
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = ConvertParameters(tool.InputSchema)
                }).ToList();
            }

            return new List<MCPToolInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering tools from {ServerName}", _config.ServerName);
            throw;
        }
    }

    public async Task<MCPToolResult> CallToolAsync(string toolName, Dictionary<string, object> arguments)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to server {_config.ServerName}");
        }

        try
        {
            var request = new JsonRpcRequest
            {
                Method = "tools/call",
                Params = new ToolCallParams
                {
                    Name = toolName,
                    Arguments = arguments
                },
                Id = NextRequestId()
            };

            var endpoint = GetEndpointFromConfig(_config);
            var response = await SendRequestAsync<ToolCallResult>(endpoint, request);
            if (response != null)
            {
                return new MCPToolResult
                {
                    Success = !response.IsError,
                    Data = ExtractContent(response.Content),
                    ErrorMessage = response.IsError ? response.Content?.FirstOrDefault()?.Text : null
                };
            }

            return new MCPToolResult
            {
                Success = false,
                ErrorMessage = "No response received"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling tool {ToolName} on {ServerName}", toolName, _config.ServerName);
            return new MCPToolResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<T?> SendRequestAsync<T>(string endpoint, JsonRpcRequest request) where T : class
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
            
        var content = new StringContent(json, Encoding.UTF8, "application/json");
            
        _logger.LogDebug("Sending JSON-RPC request: {Request}", json);
            
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
            
        var responseJson = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Received JSON-RPC response: {Response}", responseJson);
            
        var rpcResponse = JsonSerializer.Deserialize<JsonRpcResponse<T>>(responseJson, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
            
        if (rpcResponse?.Error != null)
        {
            throw new Exception($"JSON-RPC Error: {rpcResponse.Error.Message} (Code: {rpcResponse.Error.Code})");
        }
            
        return rpcResponse?.Result;
    }

    private Dictionary<string, MCPParameterInfo> ConvertParameters(JsonElement? inputSchema)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();
            
        if (inputSchema?.ValueKind == JsonValueKind.Object)
        {
            if (inputSchema.Value.TryGetProperty("properties", out var properties))
            {
                foreach (var prop in properties.EnumerateObject())
                {
                    var param = new MCPParameterInfo
                    {
                        Name = prop.Name,
                        Type = GetTypeFromSchema(prop.Value),
                        Description = prop.Value.TryGetProperty("description", out var desc) ? desc.GetString() : string.Empty,
                        Required = IsRequired(inputSchema.Value, prop.Name),
                        DefaultValue = prop.Value.TryGetProperty("default", out var def) ? def : null
                    };
                    parameters[prop.Name] = param;
                }
            }
        }
            
        return parameters;
    }

    private string GetTypeFromSchema(JsonElement schema)
    {
        if (schema.TryGetProperty("type", out var type))
        {
            return type.GetString() ?? "any";
        }
        return "any";
    }

    private bool IsRequired(JsonElement schema, string propertyName)
    {
        if (schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in required.EnumerateArray())
            {
                if (item.GetString() == propertyName)
                    return true;
            }
        }
        return false;
    }

    private object? ExtractContent(List<ContentItem>? content)
    {
        if (content == null || content.Count == 0)
            return null;
                
        if (content.Count == 1)
            return content[0].Text;
                
        return content.Select(c => c.Text ?? string.Empty).ToList();
    }

    private string NextRequestId() => (++_requestId).ToString();

    private class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
            
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
            
        [JsonPropertyName("params")]
        public object? Params { get; set; }
            
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class JsonRpcResponse<T>
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = string.Empty;
            
        [JsonPropertyName("result")]
        public T? Result { get; set; }
            
        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; set; }
            
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
            
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
            
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    private class InitializeParams
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = string.Empty;
            
        [JsonPropertyName("capabilities")]
        public ClientCapabilities Capabilities { get; set; } = new();
            
        [JsonPropertyName("clientInfo")]
        public ClientInfo ClientInfo { get; set; } = new();
    }

    private class ClientInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
            
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    private class ClientCapabilities
    {
        [JsonPropertyName("roots")]
        public object Roots { get; set; } = new { };
            
        [JsonPropertyName("sampling")]
        public object Sampling { get; set; } = new { };
            
        [JsonPropertyName("elicitation")]
        public object Elicitation { get; set; } = new { };
    }

    private class InitializeResult
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = string.Empty;
            
        [JsonPropertyName("capabilities")]
        public ServerCapabilities Capabilities { get; set; } = new();
            
        [JsonPropertyName("serverInfo")]
        public ServerInfo ServerInfo { get; set; } = new();
    }

    private class ServerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
            
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    private class ServerCapabilities
    {
        [JsonPropertyName("tools")]
        public object? Tools { get; set; }
            
        [JsonPropertyName("resources")]
        public object? Resources { get; set; }
            
        [JsonPropertyName("prompts")]
        public object? Prompts { get; set; }
    }

    private class ToolsListResult
    {
        [JsonPropertyName("tools")]
        public List<ToolInfo> Tools { get; set; } = new();
    }

    private class ToolInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
            
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
            
        [JsonPropertyName("inputSchema")]
        public JsonElement? InputSchema { get; set; }
    }

    private class ToolCallParams
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
            
        [JsonPropertyName("arguments")]
        public Dictionary<string, object> Arguments { get; set; } = new();
    }

    private class ToolCallResult
    {
        [JsonPropertyName("content")]
        public List<ContentItem>? Content { get; set; }
            
        [JsonPropertyName("isError")]
        public bool IsError { get; set; }
    }

    private class ContentItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
            
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}