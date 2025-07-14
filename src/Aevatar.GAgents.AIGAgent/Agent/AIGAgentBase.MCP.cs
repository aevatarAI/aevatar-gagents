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
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Agent;

// ReSharper disable InconsistentNaming
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
                if (!server.IsValid())
                {
                    Logger.LogWarning("Skipping invalid MCP server configuration");
                    continue;
                }

                // Create config for the MCP agent
                var mcpConfig = new MCPGAgentConfig
                {
                    Server = server
                };

                var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(mcpConfig);
                var mcpAgentId = mcpAgent.GetPrimaryKey();

                mcpAgents[server.ServerName] = new MCPGAgentReference
                {
                    AgentId = mcpAgentId,
                    ServerName = server.ServerName,
                    Description = server.Description
                };

                // Log available tools from this server
                var serverTools = await mcpAgent.GetAvailableToolsAsync();
                foreach (var (_, tool) in serverTools)
                {
                    Logger.LogInformation($"Registered MCP tool: {server.ServerName}.{tool.Name} - {tool.Description}");
                }
            }

            if (!mcpAgents.Any())
            {
                // No valid MCP servers configured
                return false;
            }

            // Update state
            var configureServersEvent = new ConfigureMCPServersStateLogEvent
            {
                MCPServers = mcpAgents
            };

            var enableMCPToolsEvent = new SetEnableMCPToolsStateLogEvent
            {
                EnableMCPTools = true
            };

            RaiseEvent(configureServersEvent);
            RaiseEvent(enableMCPToolsEvent);
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

                foreach (var (toolKey, tool) in tools)
                {
                    // toolKey already contains serverName prefix (e.g., "mcp-server-weread.get_bookshelf")
                    // Extract the actual tool name
                    var actualToolName = tool.Name;
                    var mcpToolFullName = toolKey; // Use the key as-is

                    // Use GenerateMCPFunctionName to ensure the name doesn't exceed 64 characters
                    var kernelFunctionName = GenerateMCPFunctionName(serverName, actualToolName);
                    Logger.LogInformation("MCP function name: {FunctionName} (length: {Length})", kernelFunctionName,
                        kernelFunctionName.Length);

                    // Store the mapping for later use
                    _toolNameMapping[kernelFunctionName] = mcpToolFullName;

                    var function = KernelFunctionFactory.CreateFromMethod(
                        async (KernelArguments args) => await CallMCPToolAsync(serverName, actualToolName, args),
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
                    // Clean server name to be a valid plugin name (only ASCII letters, digits, and underscores)
                    var pluginName = $"MCP_{serverName.Replace("-", "_").Replace(".", "_").Replace(" ", "_")}";

                    // Remove existing plugin with the same name to avoid duplicates
                    var existingPlugin = kernel.Plugins.FirstOrDefault(p => p.Name == pluginName);
                    if (existingPlugin != null)
                    {
                        kernel.Plugins.Remove(existingPlugin);
                        Logger.LogDebug("Removed existing MCP plugin '{PluginName}' before re-registering", pluginName);
                    }

                    kernel.Plugins.AddFromFunctions(pluginName, functions);
                    Logger.LogInformation(
                        $"Registered {functions.Count} tools from MCP server {serverName} as plugin {pluginName}");
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
    protected async Task<string> CallMCPToolAsync(string serverName, string toolName, KernelArguments kernelArgs)
    {
        var toolStartTime = DateTime.UtcNow;
        var toolCall = new ToolCallDetail
        {
            ToolName = toolName,
            ServerName = serverName,
            Arguments = kernelArgs.ToDictionary(),
            Timestamp = toolStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff UTC")
        };

        try
        {
            Logger.LogInformation($"Calling MCP tool: {serverName}.{toolName}");

            var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();

            if (!State.MCPAgents.TryGetValue(serverName, out var agentRef))
            {
                var errorMsg = $"Error: MCP server '{serverName}' not found";
                toolCall.Success = false;
                toolCall.Result = errorMsg;
                toolCall.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
                _currentToolCalls.Add(toolCall);
                return errorMsg;
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

            // Store arguments in tool call
            toolCall.Arguments = parameters;

            // Call the MCP tool with the actual tool name (not the kernel function name)
            var response = await mcpAgent.CallToolAsync(serverName, toolName, parameters);

            Logger.LogInformation($"MCP tool {serverName}.{toolName} completed");

            var result = response.Result?.ToString() ?? string.Empty;

            // Track successful tool call
            toolCall.Success = true;
            toolCall.Result = result;
            toolCall.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolCall);

            Logger.LogInformation(
                "[MCP Tool Call] {ServerName}.{ToolName} completed in {Duration}ms",
                serverName, toolName, toolCall.DurationMs);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error calling MCP tool {serverName}.{toolName}");

            var errorResult = $"Error calling tool: {ex.Message}";

            // Track failed tool call
            toolCall.Success = false;
            toolCall.Result = errorResult;
            toolCall.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolCall);

            Logger.LogError(ex,
                "[MCP Tool Call] {ServerName}.{ToolName} failed after {Duration}ms",
                serverName, toolName, toolCall.DurationMs);

            return errorResult;
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