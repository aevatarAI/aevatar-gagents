namespace Aevatar.GAgents.MCP.Options;

// ReSharper disable InconsistentNaming
[GenerateSerializer]
public class MCPServerConfig
{
    [Id(0)] public string ServerName { get; set; } = string.Empty;
    [Id(1)] public string Command { get; set; } = string.Empty;
    [Id(2)] public List<string> Args { get; set; } = [];
    [Id(3)] public Dictionary<string, string> Env { get; set; } = new();
    [Id(4)] public string Description { get; set; } = string.Empty;

    [Id(5)] public bool AutoReconnect { get; set; } = true;
    [Id(6)] public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// For SSE endpoints
    /// </summary>
    [Id(7)]
    public string? Url { get; set; }

    /// <summary>
    /// "stdio", "http", "sse"
    /// </summary>
    [Id(8)]
    public string? TransportType { get; set; }

    /// <summary>
    /// Custom initial delay for servers that need more time
    /// </summary>
    [Id(9)]
    public int? InitialDelayMs { get; set; }

    /// <summary>
    /// Custom max retries for initialization
    /// </summary>
    [Id(10)]
    public int? MaxRetries { get; set; }
    
    /// <summary>
    /// For simple SSE APIs: Custom endpoint for tool discovery (e.g., "/api/tools", "/capabilities")
    /// If not specified, defaults to "/tools"
    /// </summary>
    [Id(11)]
    public string? ToolDiscoveryEndpoint { get; set; }
    
    /// <summary>
    /// For simple SSE APIs: Predefined tools when dynamic discovery is not available
    /// </summary>
    [Id(12)]
    public List<MCPToolDefinition>? PredefinedTools { get; set; }
}

/// <summary>
/// Tool definition for predefined tools in configuration
/// </summary>
[GenerateSerializer]
public class MCPToolDefinition
{
    [Id(0)]
    public string Name { get; set; } = string.Empty;
    
    [Id(1)]
    public string Description { get; set; } = string.Empty;
    
    [Id(2)]
    public Dictionary<string, MCPParameterDefinition>? Parameters { get; set; }
}

/// <summary>
/// Parameter definition for predefined tools
/// </summary>
[GenerateSerializer]
public class MCPParameterDefinition
{
    [Id(0)]
    public string Type { get; set; } = "string";
    
    [Id(1)]
    public string? Description { get; set; }
    
    [Id(2)]
    public bool Required { get; set; }
}

public static class MCPServerConfigExtensions
{
    public static bool IsValid(this MCPServerConfig config)
    {
        return !string.IsNullOrWhiteSpace(config.ServerName);
    }
}