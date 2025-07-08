 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.Provider;
using Aevatar.GAgents.MCP.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.Examples;

/// <summary>
/// Example of using MCP GAgent with real MCP servers
/// </summary>
public class RealMCPUsageExample
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Debug);
                });

                // Add HttpClient for HTTP-based MCP servers
                services.AddHttpClient();

                // Register the real MCP client provider
                services.AddSingleton<IMCPClientProvider, RealMCPClientProvider>();

                // Register GAgent factory
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
            })
            .Build();

        await host.StartAsync();

        // Get services
        var gAgentFactory = host.Services.GetRequiredService<IGAgentFactory>();
        var logger = host.Services.GetRequiredService<ILogger<RealMCPUsageExample>>();

        try
        {
            // Example 1: Using filesystem MCP server
            await UseFilesystemServer(gAgentFactory, logger);

            // Example 2: Using multiple MCP servers
            await UseMultipleMCPServers(gAgentFactory, logger);

            // Example 3: Using HTTP-based MCP server
            await UseHttpMCPServer(gAgentFactory, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in MCP example");
        }

        await host.StopAsync();
    }

    private static async Task UseFilesystemServer(IGAgentFactory gAgentFactory, ILogger logger)
    {
        logger.LogInformation("=== Example 1: Filesystem MCP Server ===");

        // Configure MCP GAgent with filesystem server
        var config = new MCPGAgentConfig
        {
            EnableToolDiscovery = true,
            Servers = new List<MCPServerConfig>
            {
                new MCPServerConfig
                {
                    ServerName = "filesystem",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" },
                    Env = new Dictionary<string, string>()
                }
            }
        };

        // Create MCP GAgent
        var mcpGAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Discover available tools
        var discoverEvent = new MCPDiscoverToolsEvent
        {
            ServerName = "filesystem"
        };

        // NOTE: The actual usage would involve:
        // 1. Creating event handler GAgents to receive responses
        // 2. Using the GAgent's event system to publish events
        // 3. Waiting for responses through the event handlers

        // For example, to discover tools:
        // - Create a MCPDiscoverToolsEvent 
        // - Publish it through the GAgent's event system
        // - Handle the MCPToolsDiscoveredEvent response

        // To call a tool:
        // - Create a MCPToolCallEvent with server name, tool name, and arguments
        // - Publish it through the GAgent's event system  
        // - Handle the MCPToolResponseEvent response

        logger.LogInformation("MCP GAgent configured with filesystem server");
    }

    private static async Task UseMultipleMCPServers(IGAgentFactory gAgentFactory, ILogger logger)
    {
        logger.LogInformation("=== Example 2: Multiple MCP Servers ===");

        var config = new MCPGAgentConfig
        {
            EnableToolDiscovery = true,
            Servers = new List<MCPServerConfig>
            {
                new MCPServerConfig
                {
                    ServerName = "filesystem",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" }
                },
                new MCPServerConfig
                {
                    ServerName = "time",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-time" }
                },
                new MCPServerConfig
                {
                    ServerName = "memory",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-memory" }
                }
            }
        };

        var mcpGAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Get available tools from all servers
        var availableTools = await mcpGAgent.GetAvailableToolsAsync();
        logger.LogInformation("Available tools across all servers:");
        foreach (var tool in availableTools)
        {
            logger.LogInformation("  - {Server}.{Tool}: {Description}",
                tool.Value.ServerName, tool.Key, tool.Value.Description);
        }

        // Get server states  
        var serverStates = await mcpGAgent.GetServerStatesAsync();
        logger.LogInformation("Server states:");
        foreach (var state in serverStates)
        {
            logger.LogInformation("  - {Server}: Connected={Connected}",
                state.ServerName, state.IsConnected);
        }
    }

    private static async Task UseHttpMCPServer(IGAgentFactory gAgentFactory, ILogger logger)
    {
        logger.LogInformation("=== Example 3: HTTP-based MCP Server ===");

        var config = new MCPGAgentConfig
        {
            EnableToolDiscovery = true,
            RequestTimeout = TimeSpan.FromSeconds(30),
            Servers = new List<MCPServerConfig>
            {
                new MCPServerConfig
                {
                    ServerName = "custom-api",
                    Command = "http://localhost:3000/mcp", // For HTTP servers, command is the URL
                    Args = new List<string>(),
                    Env = new Dictionary<string, string>
                    {
                        ["API_KEY"] = "your-api-key"
                    }
                }
            }
        };

        var mcpGAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        logger.LogInformation("HTTP-based MCP server configured");

        // The HTTP server would expose tools via the MCP protocol
        // Tools can be discovered and called just like with stdio servers
        var availableTools = await mcpGAgent.GetAvailableToolsAsync();

        logger.LogInformation("Available tools from HTTP server: {Count}", availableTools.Count);
    }
}

// Example response handlers
[GenerateSerializer]
public class DiscoveryHandlerState : StateBase
{
    [Id(0)] public int EventCount { get; set; }
}

[GenerateSerializer]
public class DiscoveryHandlerStateLogEvent : StateLogEventBase<DiscoveryHandlerStateLogEvent>;

[GAgent("discovery_response", "sample")]
public class DiscoveryResponseHandler : GAgentBase<DiscoveryHandlerState, DiscoveryHandlerStateLogEvent>
{
    private readonly ILogger<DiscoveryResponseHandler> _logger;

    public DiscoveryResponseHandler(ILogger<DiscoveryResponseHandler> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Handles MCP tool discovery responses");
    }

    [EventHandler]
    public async Task HandleToolsDiscovered(MCPToolsDiscoveredEvent @event)
    {
        _logger.LogInformation("Discovered {Count} tools from {Server}:",
            @event.Tools.Count, @event.ServerName);

        foreach (var tool in @event.Tools)
        {
            _logger.LogInformation("  - {Name}: {Description}",
                tool.Name, tool.Description);

            if (tool.Parameters.Count > 0)
            {
                _logger.LogInformation("    Parameters:");
                foreach (var param in tool.Parameters)
                {
                    _logger.LogInformation("      - {Name} ({Type}): {Description}",
                        param.Key, param.Value.Type, param.Value.Description);
                }
            }
        }

        await Task.CompletedTask;
    }
}

[GenerateSerializer]
public class ToolHandlerState : StateBase
{
    [Id(0)] public int EventCount { get; set; }
}

[GenerateSerializer]
public class ToolHandlerStateLogEvent : StateLogEventBase<ToolHandlerStateLogEvent>;

[GAgent("tool_response", "sample")]
public class ToolResponseHandler : GAgentBase<ToolHandlerState, ToolHandlerStateLogEvent>
{
    private readonly ILogger<ToolResponseHandler> _logger;

    public ToolResponseHandler(ILogger<ToolResponseHandler> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Handles MCP tool call responses");
    }

    [EventHandler]
    public async Task HandleToolResponse(MCPToolResponseEvent @event)
    {
        if (@event.Success)
        {
            _logger.LogInformation("Tool '{Tool}' on '{Server}' succeeded with result: {Result}",
                @event.ToolName, @event.ServerName, @event.Result);
        }
        else
        {
            _logger.LogError("Tool '{Tool}' on '{Server}' failed with error: {Error}",
                @event.ToolName, @event.ServerName, @event.ErrorMessage);
        }

        await Task.CompletedTask;
    }
}