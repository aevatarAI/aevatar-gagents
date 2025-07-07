using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Agent;

/// <summary>
/// Partial class for AIGAgentBase that adds MCP (Model Context Protocol) tool capabilities
/// </summary>
public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    private Dictionary<string, string> _toolNameMapping = new(); // Maps kernel function names to MCP tool names

    /// <summary>
    /// Configure MCP servers for this agent
    /// </summary>
    public virtual async Task<bool> ConfigureMCPServersAsync(List<MCPServerConfig> servers)
    {
        try
        {
            var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
            var mcpAgents = new Dictionary<string, MCPGAgentReference>();

            foreach (var server in servers)
            {
                // Create config for the MCP agent
                var mcpConfig = new MCPGAgentConfig
                {
                    Servers = new List<MCPServerConfig> { server }
                };

                var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(mcpConfig);
                var mcpAgentId = mcpAgent.GetPrimaryKey();

                mcpAgents[server.ServerName] = new MCPGAgentReference
                {
                    AgentId = mcpAgentId,
                    ServerName = server.ServerName,
                    Description = server.ServerName
                };

                // Log available tools from this server
                var serverTools = await mcpAgent.GetAvailableToolsAsync();
                foreach (var (_, tool) in serverTools)
                {
                    var toolKey = $"{server.ServerName}.{tool.Name}";
                    Logger.LogInformation($"Registered MCP tool: {toolKey} - {tool.Description}");
                }
            }

            // Update state
            var configureServersEvent = new ConfigureMCPServersStateLogEvent
            {
                MCPServers = mcpAgents
            };

            RaiseEvent(configureServersEvent);
            await ConfirmEvents();

            // Update kernel tools if brain is initialized
            if (_brain != null)
            {
                await UpdateKernelWithMCPToolsAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure MCP servers");
            return false;
        }
    }

    /// <summary>
    /// Get available MCP tools from all configured servers
    /// </summary>
    public virtual async Task<List<MCPToolInfo>> GetAvailableMCPToolsAsync()
    {
        var allTools = new List<MCPToolInfo>();
        var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();

        foreach (var (serverName, agentRef) in State.MCPAgents)
        {
            try
            {
                var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);
                var tools = await mcpAgent.GetAvailableToolsAsync();

                foreach (var (_, tool) in tools)
                {
                    allTools.Add(tool);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to get tools from MCP server {serverName}");
            }
        }

        return allTools;
    }

    /// <summary>
    /// Update the kernel with MCP tools
    /// </summary>
    protected virtual async Task UpdateKernelWithMCPToolsAsync()
    {
        var kernel = GetKernelFromBrain();
        if (kernel == null)
        {
            Logger.LogWarning("Cannot update kernel with MCP tools: Kernel not available");
            return;
        }

        var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
        var registeredFunctions = new List<string>();

        // Clear the tool name mapping
        _toolNameMapping.Clear();

        // Register MCP tools as kernel functions
        foreach (var (serverName, agentRef) in State.MCPAgents)
        {
            try
            {
                var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);
                var tools = await mcpAgent.GetAvailableToolsAsync();

                var functions = new List<KernelFunction>();

                foreach (var (toolName, tool) in tools)
                {
                    // Semantic Kernel function names can only contain ASCII letters, digits, and underscores
                    var mcpToolFullName = $"{serverName}.{toolName}";
                    var kernelFunctionName = $"{serverName}_{toolName}".Replace(".", "_").Replace("-", "_");

                    // Store the mapping for later use
                    _toolNameMapping[kernelFunctionName] = mcpToolFullName;

                    var function = KernelFunctionFactory.CreateFromMethod(
                        async (KernelArguments args) => await CallMCPToolAsync(serverName, toolName, args),
                        functionName: kernelFunctionName,
                        description: tool.Description,
                        parameters: ConvertMCPToKernelParameters(tool.Parameters)
                    );

                    functions.Add(function);
                    registeredFunctions.Add(kernelFunctionName);
                    Logger.LogInformation($"Registered MCP tool: {kernelFunctionName} (MCP: {mcpToolFullName})");
                }

                if (functions.Any())
                {
                    kernel.Plugins.AddFromFunctions(serverName, functions);
                    Logger.LogInformation($"Registered {functions.Count} tools from MCP server {serverName}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to register tools from MCP server {serverName}");
            }
        }

        // Update state with registered functions
        if (registeredFunctions.Any())
        {
            var updateFunctionsEvent = new SetRegisteredMCPFunctionsStateLogEvent
            {
                RegisteredFunctions = registeredFunctions
            };
            RaiseEvent(updateFunctionsEvent);
            await ConfirmEvents();
        }
    }

    /// <summary>
    /// Call an MCP tool
    /// </summary>
    private async Task<string> CallMCPToolAsync(string serverName, string toolName, KernelArguments kernelArgs)
    {
        try
        {
            Logger.LogInformation($"[{DateTime.UtcNow:HH:mm:ss.fff}] Calling MCP tool: {serverName}.{toolName}");

            var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();

            if (!State.MCPAgents.TryGetValue(serverName, out var agentRef))
            {
                return $"Error: MCP server '{serverName}' not found";
            }

            var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);

            // Convert kernel arguments to basic types for Orleans serialization
            var parameters = new Dictionary<string, object>();
            foreach (var (key, value) in kernelArgs)
            {
                if (value != null)
                {
                    parameters[key] = ConvertJsonElementToBasicType(value);
                }
            }

            // Call the MCP tool with the actual tool name (not the kernel function name)
            var response = await mcpAgent.CallToolAsync(serverName, toolName, parameters);

            Logger.LogInformation($"[{DateTime.UtcNow:HH:mm:ss.fff}] MCP tool {serverName}.{toolName} completed");

            return response.Result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error calling MCP tool {serverName}.{toolName}");
            return $"Error calling tool: {ex.Message}";
        }
    }

    /// <summary>
    /// Convert MCP parameter info to Kernel parameter metadata
    /// </summary>
    private KernelParameterMetadata[] ConvertMCPToKernelParameters(Dictionary<string, MCPParameterInfo> mcpParameters)
    {
        var parameters = new List<KernelParameterMetadata>();

        foreach (var (name, paramInfo) in mcpParameters)
        {
            // Only use the basic constructor with name
            var metadata = new KernelParameterMetadata(name);
            
            // Try to set properties using reflection to handle API changes
            var metadataType = metadata.GetType();
            
            // Try to set Description property if it exists
            var descProp = metadataType.GetProperty("Description");
            if (descProp != null && descProp.CanWrite)
            {
                try
                {
                    descProp.SetValue(metadata, paramInfo.Description);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Could not set Description property on KernelParameterMetadata");
                }
            }
            
            // Try to set IsRequired property if it exists  
            var reqProp = metadataType.GetProperty("IsRequired");
            if (reqProp != null && reqProp.CanWrite)
            {
                try
                {
                    reqProp.SetValue(metadata, paramInfo.Required);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Could not set IsRequired property on KernelParameterMetadata");
                }
            }

            parameters.Add(metadata);
        }

        return parameters.ToArray();
    }

    /// <summary>
    /// Convert JsonElement to basic types for Orleans serialization
    /// </summary>
    protected object ConvertJsonElementToBasicType(object value)
    {
        if (value is JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    if (element.TryGetDouble(out double doubleValue))
                        return doubleValue;
                    return element.GetDecimal();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null!;
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElementToBasicType(item));
                    }

                    return list;
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ConvertJsonElementToBasicType(prop.Value);
                    }

                    return dict;
                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        return value;
    }

    /// <summary>
    /// State log event for configuring MCP servers
    /// </summary>
    [GenerateSerializer]
    public class ConfigureMCPServersStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Dictionary<string, MCPGAgentReference> MCPServers { get; set; } = new();
    }

    /// <summary>
    /// State log event for enabling/disabling MCP tools
    /// </summary>
    [GenerateSerializer]
    public class SetEnableMCPToolsStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public bool EnableMCPTools { get; set; }
    }

    /// <summary>
    /// State log event for setting registered MCP functions
    /// </summary>
    [GenerateSerializer]
    public class SetRegisteredMCPFunctionsStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<string> RegisteredFunctions { get; set; } = new();
    }
}