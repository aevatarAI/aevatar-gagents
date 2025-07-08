using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.Provider;

/// <summary>
/// Selects the appropriate MCP client provider based on server configuration
/// </summary>
public class MCPClientProviderSelector : IMCPClientProvider
{
    private readonly ILogger<MCPClientProviderSelector> _logger;
    private readonly StdioMCPClientProvider _stdioProvider;
    private readonly RealMCPClientProvider _httpProvider;
    private readonly Dictionary<string, IMCPClientProvider> _providerCache = new();

    public MCPClientProviderSelector(
        ILogger<MCPClientProviderSelector> logger,
        ILogger<StdioMCPClientProvider> stdioLogger,
        ILogger<RealMCPClientProvider> httpLogger,
        HttpClient httpClient)
    {
        _logger = logger;
        _stdioProvider = new StdioMCPClientProvider(stdioLogger);
        _httpProvider = new RealMCPClientProvider(httpClient, httpLogger);
        // SSE support is handled by RealMCPClientProvider with special configuration
    }

    public Task<IMCPClient> GetOrCreateClientAsync(MCPServerConfig config)
    {
        var provider = GetProviderForConfig(config);
        return provider.GetOrCreateClientAsync(config);
    }

    public Task DisconnectClientAsync(string serverName)
    {
        if (_providerCache.TryGetValue(serverName, out var provider))
        {
            return provider.DisconnectClientAsync(serverName);
        }
        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync(string serverName)
    {
        if (_providerCache.TryGetValue(serverName, out var provider))
        {
            return provider.IsConnectedAsync(serverName);
        }
        return Task.FromResult(false);
    }

    private IMCPClientProvider GetProviderForConfig(MCPServerConfig config)
    {
        // Check cache first
        if (_providerCache.TryGetValue(config.ServerName, out var cachedProvider))
        {
            return cachedProvider;
        }

        // Determine provider type based on configuration
        IMCPClientProvider provider;
        
        // Check if TransportType is explicitly specified
        if (!string.IsNullOrEmpty(config.TransportType))
        {
            switch (config.TransportType.ToLowerInvariant())
            {
                case "sse":
                    _logger.LogInformation("Using SSE provider for {ServerName}", config.ServerName);
                    // SSE is handled by HTTP provider with special SSE URL
                    if (!string.IsNullOrEmpty(config.Url))
                    {
                        config.Command = config.Url; // Use SSE URL as command
                    }
                    provider = _httpProvider;
                    break;
                case "http":
                    _logger.LogInformation("Using HTTP provider for {ServerName}", config.ServerName);
                    provider = _httpProvider;
                    break;
                case "stdio":
                    _logger.LogInformation("Using stdio provider for {ServerName}", config.ServerName);
                    provider = _stdioProvider;
                    break;
                default:
                    _logger.LogWarning("Unknown transport type {TransportType} for {ServerName}, falling back to auto-detection", 
                        config.TransportType, config.ServerName);
                    provider = AutoDetectProvider(config);
                    break;
            }
        }
        else
        {
            // Auto-detect based on command
            provider = AutoDetectProvider(config);
        }

        _providerCache[config.ServerName] = provider;
        return provider;
    }
    
    private IMCPClientProvider AutoDetectProvider(MCPServerConfig config)
    {
        // If command starts with http:// or https://, use HTTP provider
        if (config.Command.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            config.Command.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Auto-detected HTTP provider for {ServerName}", config.ServerName);
            return _httpProvider;
        }
        else
        {
            // Otherwise use stdio provider for process-based servers
            _logger.LogInformation("Auto-detected stdio provider for {ServerName}", config.ServerName);
            return _stdioProvider;
        }
    }
} 