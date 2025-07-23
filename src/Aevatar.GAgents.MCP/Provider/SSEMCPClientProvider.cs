using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.Provider;

/// <summary>
/// SSE (Server-Sent Events) implementation of MCP client provider
/// Following MCP specification with JSON-RPC 2.0 over SSE transport
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
/// SSE MCP client implementation following MCP specification
/// Client-to-server: HTTP POST requests with JSON-RPC 2.0
/// Server-to-client: SSE event stream
/// Auto-detects whether server requires MCP initialization
/// </summary>
public class SSEMCPClient : IMCPClient
{
    private readonly MCPServerConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private bool _isConnected = false;
    private CancellationTokenSource? _connectionCts;
    private Task? _eventStreamTask;
    private int _requestId = 0;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private HttpResponseMessage? _sseResponse;
    private Stream? _sseStream;
    private ServerInfo? _serverInfo;
    private ServerCapabilities? _serverCapabilities;
    private bool _isSimpleSSEApi = false; // Auto-detected flag
    private List<MCPToolInfo>? _discoveredTools = null; // Cache discovered tools
    
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
            _logger.LogInformation("Connecting to SSE MCP server {ServerName} at {Endpoint}", _config.ServerName,
                endpoint);

            // Parse the endpoint URL
            var uri = new Uri(endpoint);
            var baseUrl = uri.GetLeftPart(UriPartial.Path);

            // Extract authorization from query string if present
            var authToken = ExtractAuthToken(uri);

            // Try MCP initialization first
            var initSuccessful = await TryMcpInitializationAsync(baseUrl, authToken);

            if (!initSuccessful)
            {
                _logger.LogInformation(
                    "MCP initialization failed/not supported for {ServerName}, treating as simple SSE API",
                    _config.ServerName);
                _isSimpleSSEApi = true;

                // For simple SSE APIs, set basic server info
                _serverInfo = new ServerInfo
                {
                    Name = _config.ServerName,
                    Version = "1.0.0"
                };
                _serverCapabilities = new ServerCapabilities
                {
                    Tools = new { }
                };
            }

            // Establish SSE connection for server-to-client messages
            _connectionCts = new CancellationTokenSource();

            // Create SSE request
            var sseRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            sseRequest.Headers.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            if (!string.IsNullOrEmpty(authToken))
            {
                sseRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            // Start SSE connection
            _sseResponse = await _httpClient.SendAsync(sseRequest, HttpCompletionOption.ResponseHeadersRead,
                _connectionCts.Token);
            _sseResponse.EnsureSuccessStatusCode();

            _sseStream = await _sseResponse.Content.ReadAsStreamAsync();

            // Start processing SSE events
            _eventStreamTask = ProcessSSEEventsAsync(_sseStream, _connectionCts.Token);

            // Send initialized notification (only for full MCP servers)
            if (!_isSimpleSSEApi && initSuccessful)
            {
                await SendInitializedNotificationAsync(baseUrl, authToken);
            }

            _isConnected = true;

            ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
            {
                ServerName = _config.ServerName,
                IsConnected = true,
                Message = _isSimpleSSEApi ? "Connected to simple SSE API" : "Connected successfully via MCP SSE"
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

    private async Task<bool> TryMcpInitializationAsync(string baseUrl, string? authToken)
    {
        try
        {
            _logger.LogDebug("Attempting MCP initialization for {ServerName}", _config.ServerName);

            var initRequest = new JsonRpcRequest
            {
                JsonRpc = "2.0",
                Id = NextRequestId(),
                Method = "initialize",
                Params = new Dictionary<string, object>
                {
                    ["protocolVersion"] = "2025-06-18",
                    ["clientInfo"] = new Dictionary<string, object>
                    {
                        ["name"] = "AevatarMCPClient",
                        ["version"] = "1.0.0"
                    },
                    ["capabilities"] = new Dictionary<string, object>
                    {
                        ["roots"] = new { },
                        ["sampling"] = new { },
                        ["elicitation"] = new { }
                    }
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(initRequest, GetJsonOptions()),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            if (!string.IsNullOrEmpty(authToken))
            {
                httpRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            // Send initialize request with a shorter timeout for auto-detection
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.SendAsync(httpRequest, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Initialize request failed with status {StatusCode}", response.StatusCode);
                return false;
            }

            // Parse initialize response
            var responseContent = await response.Content.ReadAsStringAsync();
            var initResponse =
                JsonSerializer.Deserialize<JsonRpcResponse<InitializeResult>>(responseContent, GetJsonOptions());

            if (initResponse?.Result != null)
            {
                _serverInfo = initResponse.Result.ServerInfo;
                _serverCapabilities = initResponse.Result.Capabilities;

                _logger.LogInformation("Successfully initialized MCP connection to {ServerName}: {ServerInfo}",
                    _config.ServerName, _serverInfo?.Name);
                return true;
            }
            else if (initResponse?.Error != null)
            {
                _logger.LogDebug("Initialize returned error: {Error}", initResponse.Error.Message);
                return false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "MCP initialization failed for {ServerName}", _config.ServerName);
            return false;
        }
    }

    private async Task SendInitializedNotificationAsync(string baseUrl, string? authToken)
    {
        try
        {
            var initializedNotification = new JsonRpcNotification
            {
                JsonRpc = "2.0",
                Method = "initialized",
                Params = new { }
            };

            var notifyRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(initializedNotification, GetJsonOptions()),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            if (!string.IsNullOrEmpty(authToken))
            {
                notifyRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            await _httpClient.SendAsync(notifyRequest);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send initialized notification, continuing anyway");
        }
    }

    private async Task ProcessSSEEventsAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(stream);
            string? eventType = null;
            var dataBuilder = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break; // End of stream

                if (line.StartsWith("event: "))
                {
                    eventType = line.Substring(7);
                }
                else if (line.StartsWith("data: "))
                {
                    dataBuilder.AppendLine(line.Substring(6));
                }
                else if (string.IsNullOrEmpty(line) && dataBuilder.Length > 0)
                {
                    // End of event, process it
                    var data = dataBuilder.ToString().Trim();
                    dataBuilder.Clear();

                    if (eventType == "message" && !string.IsNullOrEmpty(data))
                    {
                        try
                        {
                            var jsonRpcMessage = JsonSerializer.Deserialize<JsonElement>(data, GetJsonOptions());

                            // Check if it's a response (has id) or notification (no id)
                            if (jsonRpcMessage.TryGetProperty("id", out var idElement))
                            {
                                var id = idElement.ToString();
                                if (_pendingRequests.TryRemove(id, out var tcs))
                                {
                                    tcs.SetResult(jsonRpcMessage);
                                }
                                else
                                {
                                    _logger.LogWarning("Received response for unknown request ID: {Id}", id);
                                }
                            }
                            else
                            {
                                // It's a notification
                                _logger.LogDebug("Received notification: {Data}", data);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Failed to parse SSE data as JSON: {Data}", data);
                        }
                    }
                    else if (eventType == "tools" && !string.IsNullOrEmpty(data) && _isSimpleSSEApi)
                    {
                        // Handle tools discovery event for simple SSE APIs
                        try
                        {
                            var toolsData = JsonSerializer.Deserialize<JsonElement>(data, GetJsonOptions());
                            var tools = new List<MCPToolInfo>();
                            
                            if (toolsData.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var tool in toolsData.EnumerateArray())
                                {
                                    tools.Add(ParseToolInfo(tool));
                                }
                            }
                            else if (toolsData.TryGetProperty("tools", out var toolsArray) && 
                                     toolsArray.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var tool in toolsArray.EnumerateArray())
                                {
                                    tools.Add(ParseToolInfo(tool));
                                }
                            }
                            
                            if (tools.Count > 0)
                            {
                                _discoveredTools = tools;
                                _logger.LogInformation("Discovered {Count} tools from SSE event stream for {ServerName}", 
                                    tools.Count, _config.ServerName);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Failed to parse tools event data: {Data}", data);
                        }
                    }

                    eventType = null;
                }
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing SSE events for {ServerName}", _config.ServerName);
            }
        }
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

        _sseStream?.Dispose();
        _sseResponse?.Dispose();
        _connectionCts?.Dispose();
        _isConnected = false;

        // Cancel all pending requests
        foreach (var pending in _pendingRequests)
        {
            pending.Value.TrySetCanceled();
        }

        _pendingRequests.Clear();

        ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
        {
            ServerName = _config.ServerName,
            IsConnected = false,
            Message = "Disconnected"
        });

        _logger.LogInformation("Disconnected from SSE server {ServerName}", _config.ServerName);
    }

    public async Task<List<MCPToolInfo>> DiscoverToolsAsync()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to server {_config.ServerName}");
        }

        try
        {
            // For simple SSE APIs, extract tool from URL or use predefined tools
            if (_isSimpleSSEApi)
            {
                _logger.LogInformation("Discovering tools for simple SSE API {ServerName}", _config.ServerName);
                
                // Try to extract tool name from URL pattern like /api/mcp/{tool_name}/sse
                var endpoint = GetEndpointFromConfig(_config);
                var uri = new Uri(endpoint);
                var path = uri.AbsolutePath;
                
                // Common patterns: /api/mcp/web_search/sse, /mcp/tool_name/sse, etc.
                var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string? toolName = null;
                
                // Look for 'mcp' in path and get the next segment as tool name
                for (int i = 0; i < pathSegments.Length - 1; i++)
                {
                    if (pathSegments[i].Equals("mcp", StringComparison.OrdinalIgnoreCase))
                    {
                        // Next segment is likely the tool name
                        if (i + 1 < pathSegments.Length && !pathSegments[i + 1].Equals("sse", StringComparison.OrdinalIgnoreCase))
                        {
                            toolName = pathSegments[i + 1];
                            break;
                        }
                    }
                }
                
                // If we found a tool name in the URL
                if (!string.IsNullOrEmpty(toolName))
                {
                    _logger.LogInformation("Extracted tool '{ToolName}' from URL for {ServerName}", toolName, _config.ServerName);
                    
                    var tools = new List<MCPToolInfo>
                    {
                        new MCPToolInfo
                        {
                            Name = toolName,
                            Description = $"{toolName} tool",
                            Parameters = new Dictionary<string, MCPParameterInfo>
                            {
                                ["query"] = new MCPParameterInfo
                                {
                                    Name = "query",
                                    Type = "string",
                                    Description = "The query parameter",
                                    Required = true
                                }
                            }
                        }
                    };
                    
                    _discoveredTools = tools;
                    return tools;
                }
                
                // Check if tools were discovered from SSE event stream
                if (_discoveredTools != null && _discoveredTools.Count > 0)
                {
                    return _discoveredTools;
                }
                
                // Use predefined tools from configuration
                if (_config.PredefinedTools != null && _config.PredefinedTools.Count > 0)
                {
                    var predefinedTools = _config.PredefinedTools.Select(t => new MCPToolInfo
                    {
                        Name = t.Name,
                        Description = t.Description,
                        Parameters = t.Parameters?.ToDictionary(
                            p => p.Key,
                            p => new MCPParameterInfo
                            {
                                Name = p.Key,
                                Type = p.Value.Type,
                                Description = p.Value.Description,
                                Required = p.Value.Required
                            }
                        ) ?? new Dictionary<string, MCPParameterInfo>()
                    }).ToList();
                    
                    _discoveredTools = predefinedTools;
                    return predefinedTools;
                }
                
                // No tools discovered
                _logger.LogWarning("No tools discovered for simple SSE API {ServerName}", _config.ServerName);
                return new List<MCPToolInfo>();
            }
            
            // For full MCP servers, use standard tool discovery
            var request = new JsonRpcRequest
            {
                JsonRpc = "2.0",
                Id = NextRequestId(),
                Method = "tools/list",
                Params = new { }
            };

            var response = await SendRequestAsync<ToolsListResult>(request);
            
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


    private MCPToolInfo ParseToolInfo(JsonElement toolElement)
    {
        var tool = new MCPToolInfo
        {
            Name = toolElement.GetProperty("name").GetString() ?? "unknown",
            Description = toolElement.TryGetProperty("description", out var desc) ? desc.GetString() : null
        };

        // Parse parameters if present
        if (toolElement.TryGetProperty("parameters", out var parameters))
        {
            tool.Parameters = new Dictionary<string, MCPParameterInfo>();

            if (parameters.ValueKind == JsonValueKind.Object)
            {
                foreach (var param in parameters.EnumerateObject())
                {
                    tool.Parameters[param.Name] = new MCPParameterInfo
                    {
                        Name = param.Name,
                        Type = param.Value.TryGetProperty("type", out var type) ? type.GetString() : "string",
                        Description = param.Value.TryGetProperty("description", out var paramDesc)
                            ? paramDesc.GetString()
                            : null,
                        Required = param.Value.TryGetProperty("required", out var req) && req.GetBoolean()
                    };
                }
            }
        }

        return tool;
    }

    public async Task<MCPToolResult> CallToolAsync(string toolName, Dictionary<string, object> arguments)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to server {_config.ServerName}");
        }

        try
        {
            // For simple SSE APIs, use direct HTTP POST without JSON-RPC
            if (_isSimpleSSEApi)
            {
                _logger.LogInformation("Calling tool {ToolName} on simple SSE API {ServerName}", toolName,
                    _config.ServerName);

                var endpoint = GetEndpointFromConfig(_config);
                var uri = new Uri(endpoint);
                var baseUrl = uri.GetLeftPart(UriPartial.Path);
                var authToken = ExtractAuthToken(uri);

                // For simple SSE APIs, send arguments directly
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(arguments, GetJsonOptions()),
                        Encoding.UTF8,
                        "application/json"
                    )
                };

                if (!string.IsNullOrEmpty(authToken))
                {
                    httpRequest.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                }

                // Send request and parse SSE response
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response =
                    await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new MCPToolResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                    };
                }

                // Read SSE events directly for simple APIs
                var resultData = new StringBuilder();
                await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream && !cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cts.Token);
                    if (line == null) break;

                    if (line.StartsWith("data: "))
                    {
                        resultData.AppendLine(line.Substring(6));
                    }
                    else if (string.IsNullOrEmpty(line) && resultData.Length > 0)
                    {
                        // End of event, process accumulated data
                        break;
                    }
                }

                var result = resultData.ToString().Trim();
                if (!string.IsNullOrEmpty(result))
                {
                    try
                    {
                        // Try to parse as JSON
                        var jsonResult =
                            JsonSerializer.Deserialize<Dictionary<string, object>>(result, GetJsonOptions());
                        return new MCPToolResult
                        {
                            Success = true,
                            Data = jsonResult
                        };
                    }
                    catch
                    {
                        // If not JSON, return as text
                        return new MCPToolResult
                        {
                            Success = true,
                            Data = result
                        };
                    }
                }

                return new MCPToolResult
                {
                    Success = false,
                    ErrorMessage = "No data received"
                };
            }

            // For full MCP servers, use standard JSON-RPC tool calling
            var request = new JsonRpcRequest
            {
                JsonRpc = "2.0",
                Id = NextRequestId(),
                Method = "tools/call",
                Params = new Dictionary<string, object>
                {
                    ["name"] = toolName,
                    ["arguments"] = arguments
                }
            };

            var rpcResponse = await SendRequestAsync<ToolCallResult>(request);

            if (rpcResponse != null)
            {
                return new MCPToolResult
                {
                    Success = !rpcResponse.IsError,
                    Data = ExtractContent(rpcResponse.Content),
                    ErrorMessage = rpcResponse.IsError ? rpcResponse.Content?.FirstOrDefault()?.Text : null
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

    private async Task<T?> SendRequestAsync<T>(JsonRpcRequest request) where T : class
    {
        var endpoint = GetEndpointFromConfig(_config);
        var uri = new Uri(endpoint);
        var baseUrl = uri.GetLeftPart(UriPartial.Path);
        var authToken = ExtractAuthToken(uri);

        // Register the pending request
        var requestId = request.Id;
        var tcs = new TaskCompletionSource<JsonElement>();
        _pendingRequests[requestId] = tcs;

        try
        {
            // Send HTTP POST request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request, GetJsonOptions()),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            if (!string.IsNullOrEmpty(authToken))
            {
                httpRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            _logger.LogDebug("Sending SSE JSON-RPC request: {Method} with ID {Id}", request.Method, request.Id);

            // Send the request (fire and forget, response will come via SSE)
            var postResponse = await _httpClient.SendAsync(httpRequest);

            // For SSE, the POST might just acknowledge receipt
            if (!postResponse.IsSuccessStatusCode)
            {
                var errorContent = await postResponse.Content.ReadAsStringAsync();
                throw new Exception($"Request failed with status {postResponse.StatusCode}: {errorContent}");
            }

            // Wait for the response via SSE with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var responseJson = await tcs.Task.WaitAsync(cts.Token);

            // Parse the response
            var rpcResponse =
                JsonSerializer.Deserialize<JsonRpcResponse<T>>(responseJson.GetRawText(), GetJsonOptions());

            if (rpcResponse?.Error != null)
            {
                throw new Exception($"JSON-RPC Error: {rpcResponse.Error.Message} (Code: {rpcResponse.Error.Code})");
            }

            return rpcResponse?.Result;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Request timed out waiting for response to {request.Method}");
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private string GetEndpointFromConfig(MCPServerConfig config)
    {
        if (!string.IsNullOrEmpty(config.Url))
        {
            return config.Url;
        }

        return config.Command;
    }

    private string? ExtractAuthToken(Uri uri)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["Authorization"] ?? query["authorization"] ?? query["auth"] ?? query["token"];
    }

    private string NextRequestId()
    {
        return (++_requestId).ToString();
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    private Dictionary<string, MCPParameterInfo> ConvertParameters(JsonElement? inputSchema)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();

        if (inputSchema.HasValue &&
            inputSchema.Value.TryGetProperty("properties", out var properties))
        {
            var required = new HashSet<string>();
            if (inputSchema.Value.TryGetProperty("required", out var requiredArray))
            {
                foreach (var item in requiredArray.EnumerateArray())
                {
                    required.Add(item.GetString() ?? string.Empty);
                }
            }

            foreach (var prop in properties.EnumerateObject())
            {
                var paramInfo = new MCPParameterInfo
                {
                    Name = prop.Name,
                    Type = prop.Value.TryGetProperty("type", out var type) ? type.GetString() ?? "string" : "string",
                    Description = prop.Value.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    Required = required.Contains(prop.Name)
                };

                parameters[prop.Name] = paramInfo;
            }
        }

        return parameters;
    }

    private object? ExtractContent(List<ToolContent>? content)
    {
        if (content == null || content.Count == 0)
            return null;

        if (content.Count == 1)
            return content[0].Text;

        return content.Select(c => c.Text).ToList();
    }
}

// JSON-RPC message classes
internal class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

internal class JsonRpcNotification
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

internal class JsonRpcResponse<T>
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

internal class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

// MCP specific result classes
internal class InitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = string.Empty;

    [JsonPropertyName("serverInfo")]
    public ServerInfo ServerInfo { get; set; } = new();

    [JsonPropertyName("capabilities")]
    public ServerCapabilities Capabilities { get; set; } = new();
}

internal class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

internal class ServerCapabilities
{
    [JsonPropertyName("tools")]
    public object? Tools { get; set; }

    [JsonPropertyName("resources")]
    public object? Resources { get; set; }

    [JsonPropertyName("prompts")]
    public object? Prompts { get; set; }
}

internal class ToolsListResult
{
    [JsonPropertyName("tools")]
    public List<ToolInfo> Tools { get; set; } = new();
}

internal class ToolInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public JsonElement? InputSchema { get; set; }
}

internal class ToolCallResult
{
    [JsonPropertyName("content")]
    public List<ToolContent> Content { get; set; } = new();

    [JsonPropertyName("isError")]
    public bool IsError { get; set; }
}

internal class ToolContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}