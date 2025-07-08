using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.Provider;

/// <summary>
/// MCP client provider implementation using JSON-RPC over stdio
/// </summary>
public class StdioMCPClientProvider : IMCPClientProvider
{
    private readonly ILogger<StdioMCPClientProvider> _logger;
    private readonly Dictionary<string, StdioMCPClient> _clients = new();

    public StdioMCPClientProvider(ILogger<StdioMCPClientProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IMCPClient> GetOrCreateClientAsync(MCPServerConfig config)
    {
        if (_clients.TryGetValue(config.ServerName, out var existingClient))
        {
            return Task.FromResult<IMCPClient>(existingClient);
        }

        var client = new StdioMCPClient(config, _logger);
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
/// MCP client implementation using JSON-RPC over stdio
/// </summary>
public class StdioMCPClient : IMCPClient
{
    private readonly MCPServerConfig _config;
    private readonly ILogger _logger;
    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private int _requestId = 0;
    private bool _isConnected = false;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readTask;
    
    // Configurable timeouts
    private readonly TimeSpan _processStartTimeout = TimeSpan.FromMinutes(2); // 2 minutes for npx to download
    private readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(60); // 60 seconds for regular requests

    public bool IsConnected => _isConnected;

    public event EventHandler<MCPConnectionStatusEventArgs>? ConnectionStatusChanged;

    public StdioMCPClient(MCPServerConfig config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            var args = _config.Args ?? new List<string>();
            _logger.LogInformation("Starting MCP server {ServerName} with command: {Command} {Args}", 
                _config.ServerName, _config.Command, string.Join(" ", args));

            // Start the process
            var startInfo = new ProcessStartInfo
            {
                FileName = _config.Command,
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Add environment variables if specified
            if (_config.Environment != null)
            {
                foreach (var kvp in _config.Environment)
                {
                    startInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            _process = Process.Start(startInfo);
            if (_process == null)
            {
                throw new Exception("Failed to start process");
            }

            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;
            
            // Start reading responses
            _cancellationTokenSource = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadResponsesAsync(_cancellationTokenSource.Token));
            
            // Start reading stderr to log any errors
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_process.StandardError.EndOfStream)
                    {
                        var line = await _process.StandardError.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            _logger.LogWarning("MCP server {ServerName} stderr: {Message}", _config.ServerName, line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error reading stderr for {ServerName}", _config.ServerName);
                }
            });

            // Wait a bit for the process to start
            await Task.Delay(1000);

            // Send initialize request
            var initRequest = new
            {
                jsonrpc = "2.0",
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        roots = new { },
                        sampling = new { }
                    },
                    clientInfo = new
                    {
                        name = "AevatarMCPClient",
                        version = "1.0.0"
                    }
                },
                id = NextRequestId()
            };

            var response = await SendRequestAsync(initRequest, _processStartTimeout);
            if (response.TryGetProperty("serverInfo", out var serverInfo))
            {
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

    public async Task DisconnectAsync()
    {
        _isConnected = false;
        
        // Cancel read task
        _cancellationTokenSource?.Cancel();
        if (_readTask != null)
        {
            try
            {
                await _readTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        // Cleanup process
        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill();
                _process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error killing process for {ServerName}", _config.ServerName);
            }
        }

        _stdin?.Dispose();
        _stdout?.Dispose();
        _process?.Dispose();
        _cancellationTokenSource?.Dispose();

        // Clear pending requests
        foreach (var pending in _pendingRequests.Values)
        {
            pending.TrySetCanceled();
        }
        _pendingRequests.Clear();
            
        ConnectionStatusChanged?.Invoke(this, new MCPConnectionStatusEventArgs
        {
            ServerName = _config.ServerName,
            IsConnected = false,
            Message = "Disconnected"
        });
            
        _logger.LogInformation("Disconnected from {ServerName}", _config.ServerName);
    }

    public async Task<List<MCPToolInfo>> DiscoverToolsAsync()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to server {_config.ServerName}");
        }

        try
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "tools/list",
                @params = new { },
                id = NextRequestId()
            };

            var response = await SendRequestAsync(request);
            if (response.TryGetProperty("tools", out var tools))
            {
                var toolList = new List<MCPToolInfo>();
                foreach (var tool in tools.EnumerateArray())
                {
                    toolList.Add(new MCPToolInfo
                    {
                        Name = tool.GetProperty("name").GetString() ?? string.Empty,
                        Description = tool.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                        Parameters = ConvertParameters(tool.TryGetProperty("inputSchema", out var schema) ? schema : default)
                    });
                }
                return toolList;
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
            var request = new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments = arguments
                },
                id = NextRequestId()
            };

            var response = await SendRequestAsync(request);
            
            // Check for error
            if (response.TryGetProperty("error", out var error))
            {
                var errorMessage = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
                return new MCPToolResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            // Extract content
            if (response.TryGetProperty("content", out var content))
            {
                var contentList = new List<string>();
                foreach (var item in content.EnumerateArray())
                {
                    if (item.TryGetProperty("text", out var text))
                    {
                        contentList.Add(text.GetString() ?? string.Empty);
                    }
                }
                
                return new MCPToolResult
                {
                    Success = true,
                    Data = contentList.Count == 1 ? contentList[0] : contentList
                };
            }

            return new MCPToolResult
            {
                Success = false,
                ErrorMessage = "No content in response"
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

    private async Task<JsonElement> SendRequestAsync(object request)
    {
        return await SendRequestAsync(request, _requestTimeout);
    }

    private async Task<JsonElement> SendRequestAsync(object request, TimeSpan timeout)
    {
        var requestId = GetRequestId(request);
        var tcs = new TaskCompletionSource<JsonElement>();
        _pendingRequests[requestId] = tcs;

        try
        {
            var json = JsonSerializer.Serialize(request);
            _logger.LogDebug("Sending JSON-RPC request: {Request}", json);
            
            await _stdin!.WriteLineAsync(json);
            await _stdin.FlushAsync();

            // Wait for response with configurable timeout
            using var cts = new CancellationTokenSource(timeout);
            using var registration = cts.Token.Register(() => tcs.TrySetCanceled());
            
            var response = await tcs.Task;
            return response;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private async Task ReadResponsesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_stdout!.EndOfStream)
            {
                var line = await _stdout.ReadLineAsync();
                if (string.IsNullOrEmpty(line))
                    continue;

                _logger.LogDebug("Received JSON-RPC response: {Response}", line);

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("id", out var id))
                    {
                        var requestId = id.ValueKind == JsonValueKind.Number 
                            ? id.GetInt32().ToString() 
                            : id.GetString() ?? string.Empty;
                            
                        if (_pendingRequests.TryRemove(requestId, out var tcs))
                        {
                            // Clone the element since the document will be disposed
                            var response = root.Clone();
                            
                            if (root.TryGetProperty("result", out var result))
                            {
                                tcs.TrySetResult(result.Clone());
                            }
                            else if (root.TryGetProperty("error", out var error))
                            {
                                tcs.TrySetResult(response);
                            }
                            else
                            {
                                tcs.TrySetResult(response);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing JSON-RPC response");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in response reader");
        }
    }

    private Dictionary<string, MCPParameterInfo> ConvertParameters(JsonElement inputSchema)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();
            
        if (inputSchema.ValueKind == JsonValueKind.Object)
        {
            if (inputSchema.TryGetProperty("properties", out var properties))
            {
                foreach (var prop in properties.EnumerateObject())
                {
                    var param = new MCPParameterInfo
                    {
                        Name = prop.Name,
                        Type = GetTypeFromSchema(prop.Value),
                        Description = prop.Value.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                        Required = IsRequired(inputSchema, prop.Name),
                        DefaultValue = prop.Value.TryGetProperty("default", out var def) ? ConvertJsonElementToBasicType(def) : null
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
            // Handle both string and array types
            if (type.ValueKind == JsonValueKind.String)
            {
                return type.GetString() ?? "any";
            }
            else if (type.ValueKind == JsonValueKind.Array)
            {
                // For array types (e.g., ["string", "null"]), take the first non-null type
                foreach (var item in type.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var typeStr = item.GetString();
                        if (typeStr != null && typeStr != "null")
                        {
                            return typeStr;
                        }
                    }
                }
            }
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

    private string NextRequestId() => (++_requestId).ToString();

    private string GetRequestId(object request)
    {
        var json = JsonSerializer.Serialize(request);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("id", out var id))
        {
            return id.ValueKind == JsonValueKind.Number 
                ? id.GetInt32().ToString() 
                : id.GetString() ?? string.Empty;
        }
        return string.Empty;
    }
    
    private object? ConvertJsonElementToBasicType(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                if (element.TryGetDouble(out var doubleValue))
                    return doubleValue;
                return element.GetDecimal();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElementToBasicType(item));
                }
                return list;
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElementToBasicType(prop.Value);
                }
                return dict;
            default:
                return element.ToString();
        }
    }
} 