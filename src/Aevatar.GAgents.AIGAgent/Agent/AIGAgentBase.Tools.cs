using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Plugin;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.GAgents.AIGAgent.Agent;

/// <summary>
/// Partial class for AIGAgentBase that adds GAgent tool registration capabilities
/// </summary>
public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    private IGAgentService? _gAgentService;
    private IGAgentExecutor? _gAgentExecutor;
    private GAgentToolPlugin? _gAgentToolPlugin;
    private List<ToolCallDetail> _currentToolCalls = new(); // Track tool calls for current request

    /// <summary>
    /// Gets the current tool calls for tracking
    /// </summary>
    protected List<ToolCallDetail> CurrentToolCalls => _currentToolCalls;

    /// <summary>
    /// Clears the current tool calls tracking
    /// </summary>
    protected void ClearToolCalls()
    {
        _currentToolCalls.Clear();
    }

    /// <summary>
    /// Registers all available GAgents as tools in the Semantic Kernel
    /// </summary>
    protected virtual async Task RegisterGAgentsAsToolsAsync()
    {
        if (_brain == null)
        {
            Logger.LogWarning("Cannot register GAgent tools: Brain not initialized");
            return;
        }

        if (!State.EnableGAgentTools)
        {
            Logger.LogInformation("GAgent tools are disabled");
            return;
        }

        try
        {
            _gAgentService ??= ServiceProvider.GetRequiredService<IGAgentService>();
            _gAgentExecutor ??= ServiceProvider.GetRequiredService<IGAgentExecutor>();

            Logger.LogInformation("Starting GAgent tools registration");

            // Create GAgent tool plugin
            _gAgentToolPlugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, Logger);

            // Get the kernel from brain using reflection
            var kernel = GetKernelFromBrain();
            if (kernel == null)
            {
                Logger.LogWarning("Cannot access Semantic Kernel from brain");
                return;
            }

            // Import the plugin with its built-in functions
            ImportPluginToKernel(kernel, _gAgentToolPlugin, "GAgentTools");

            // Get all available GAgents
            var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();

            // Create dynamic functions for each GAgent event
            var registeredFunctions = await RegisterDynamicGAgentFunctionsAsync(kernel, allGAgents);

            Logger.LogInformation("Successfully registered {Count} GAgent functions as tools",
                registeredFunctions.Count);

            // Store registered function names in state directly
            var functionNames = registeredFunctions.Select(f => f.Name).ToList();
            State.RegisteredGAgentFunctions = functionNames;

            // Persist state changes
            await ConfirmEvents();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register GAgent tools");
            throw;
        }
    }

    /// <summary>
    /// Creates a state log event for setting registered functions
    /// </summary>
    private TStateLogEvent? CreateSetRegisteredFunctionsEvent(List<string> functionNames)
    {
        // This is a workaround - in real implementation, we should have a proper event type
        // that inherits from TStateLogEvent
        if (typeof(TStateLogEvent).IsAssignableFrom(typeof(SetRegisteredGAgentFunctionsStateLogEvent)))
        {
            var evt = new SetRegisteredGAgentFunctionsStateLogEvent
            {
                RegisteredFunctions = functionNames
            };
            return evt as TStateLogEvent;
        }

        Logger.LogWarning("Cannot create SetRegisteredGAgentFunctionsStateLogEvent - incompatible event type");
        return null;
    }

    /// <summary>
    /// Registers dynamic functions for each GAgent and their events
    /// </summary>
    private async Task<List<KernelFunction>> RegisterDynamicGAgentFunctionsAsync(Kernel kernel,
        Dictionary<GrainType, List<Type>> allGAgents)
    {
        var dynamicFunctions = new List<KernelFunction>();

        // Remove existing GAgent plugins to avoid duplicates
        var existingGAgentPlugins = kernel.Plugins.Where(p => p.Name.StartsWith("GA_")).ToList();
        foreach (var plugin in existingGAgentPlugins)
        {
            kernel.Plugins.Remove(plugin);
        }

        foreach (var (grainType, eventTypes) in allGAgents)
        {
            try
            {
                // Get GAgent description
                var gAgentInfo = await _gAgentService!.GetGAgentDetailInfoAsync(grainType);
                var gAgentDescription = gAgentInfo?.Description ?? "GAgent";
                var functions = new List<KernelFunction>();

                foreach (var eventType in eventTypes)
                {
                    var functionName = GenerateFunctionName(grainType, eventType);
                    var functionDescription = GenerateFunctionDescription(grainType, eventType, gAgentDescription);

                    // Create the kernel function with tracking wrapper
                    var function = KernelFunctionFactory.CreateFromMethod(
                        async (KernelArguments args) =>
                        {
                            var toolStartTime = DateTime.UtcNow;
                            var toolCall = new ToolCallDetail
                            {
                                ToolName = functionName,
                                ServerName = grainType.ToString() ?? "GAgent",
                                Arguments = args.ToDictionary(),
                                Timestamp = toolStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff UTC")
                            };

                            try
                            {
                                // Call the actual GAgent tool
                                var result = await CallGAgentToolAsync(grainType, eventType, args);

                                toolCall.Result = JsonSerializer.Serialize(result);
                                toolCall.Success = true;
                                toolCall.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;

                                // Add to tracking
                                _currentToolCalls.Add(toolCall);

                                Logger.LogInformation(
                                    "[GAgent Tool Call] {GrainType}.{EventType} completed in {Duration}ms",
                                    grainType, eventType.Name, toolCall.DurationMs);

                                return result;
                            }
                            catch (Exception ex)
                            {
                                toolCall.Success = false;
                                toolCall.Result = $"Error: {ex.Message}";
                                toolCall.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;

                                // Add to tracking even if failed
                                _currentToolCalls.Add(toolCall);

                                Logger.LogError(ex,
                                    "[GAgent Tool Call] {GrainType}.{EventType} failed after {Duration}ms",
                                    grainType, eventType.Name, toolCall.DurationMs);
                                throw;
                            }
                        },
                        functionName,
                        functionDescription);

                    // Set parameters from event properties
                    SetKernelFunctionParametersFromEventType(function, eventType);
                    functions.Add(function);
                    dynamicFunctions.Add(function);
                }

                // Create plugin for this GAgent
                if (functions.Count > 0)
                {
                    // Generate a short plugin name to avoid exceeding OpenAI's 64-char limit
                    // when combined with function names
                    var fullGrainType = grainType.ToString() ?? "Unknown";
                    var cleanGrainType = fullGrainType.Replace("/", "_").Replace(".", "_").Replace("-", "_");

                    // Start with a short prefix
                    var pluginName = "GA_";

                    // Try to extract the most meaningful part of the grain type
                    var parts = fullGrainType.Split('/');
                    if (parts.Length > 1)
                    {
                        // Use the last part after '/' (e.g., "chatgagent" from "demo/chatgagent")
                        var lastPart = parts[^1].Replace(".", "_").Replace("-", "_");
                        pluginName += lastPart;
                    }
                    else
                    {
                        // Try to shorten long type names
                        var typeParts = cleanGrainType.Split('_');
                        if (typeParts.Length > 2)
                        {
                            // Take the last meaningful part
                            pluginName += typeParts[typeParts.Length - 1];
                        }
                        else
                        {
                            pluginName += cleanGrainType;
                        }
                    }

                    // Ensure plugin name is not too long (max 20 chars to leave room for function names)
                    if (pluginName.Length > 20)
                    {
                        // Generate a more unique hash using the full grain type
                        var hashBytes = System.Text.Encoding.UTF8.GetBytes(fullGrainType);
                        using (var sha = System.Security.Cryptography.SHA256.Create())
                        {
                            var hash = sha.ComputeHash(hashBytes);
                            var shortHash = Convert.ToBase64String(hash).Substring(0, 8).Replace("/", "_")
                                .Replace("+", "_");
                            pluginName = $"GA_{shortHash}";
                        }
                    }

                    // Check if plugin already exists and remove it
                    var existingPlugin = kernel.Plugins.FirstOrDefault(p => p.Name == pluginName);
                    if (existingPlugin != null)
                    {
                        kernel.Plugins.Remove(existingPlugin);
                        Logger.LogDebug("Removed existing plugin '{PluginName}' before adding new functions",
                            pluginName);
                    }

                    try
                    {
                        kernel.Plugins.AddFromFunctions(pluginName, functions.DistinctBy(f => f.Name).ToList());
                        Logger.LogInformation(
                            "Registered GAgent plugin '{PluginName}' with {ToolCount} tools (original: {OriginalType})",
                            pluginName, functions.Count, grainType);
                    }
                    catch (ArgumentException ex) when (ex.Message.Contains("already been added"))
                    {
                        // This can happen in race conditions, log it but continue
                        Logger.LogWarning(
                            "Plugin '{PluginName}' already exists (race condition), skipping registration for {GrainType}",
                            pluginName, grainType);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to register functions for GAgent {GrainType}", grainType);
            }
        }

        return dynamicFunctions;
    }

    /// <summary>
    /// Gets the Semantic Kernel from the brain using reflection
    /// </summary>
    protected Kernel? GetKernelFromBrain()
    {
        if (_brain == null)
            return null;

        try
        {
            var brainType = _brain.GetType();
            var kernelField = brainType.GetField("Kernel",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (kernelField != null)
            {
                return kernelField.GetValue(_brain) as Kernel;
            }

            var kernelProperty = brainType.GetProperty("Kernel",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (kernelProperty != null)
            {
                return kernelProperty.GetValue(_brain) as Kernel;
            }

            Logger.LogWarning("Cannot find Kernel field or property in brain type {BrainType}", brainType.Name);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error accessing Kernel from brain");
            return null;
        }
    }

    /// <summary>
    /// Imports a plugin to the kernel using reflection
    /// </summary>
    private void ImportPluginToKernel(Kernel kernel, object plugin, string pluginName)
    {
        try
        {
            // Remove existing plugin with the same name to avoid duplicates
            var existingPlugin = kernel.Plugins.FirstOrDefault(p => p.Name == pluginName);
            if (existingPlugin != null)
            {
                kernel.Plugins.Remove(existingPlugin);
                Logger.LogDebug("Removed existing plugin '{PluginName}' before re-importing", pluginName);
            }

            // Use the modern API directly
            kernel.Plugins.AddFromObject(plugin, pluginName);
            Logger.LogInformation("Successfully imported plugin '{PluginName}' to kernel", pluginName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error importing plugin to kernel");
        }
    }

    /// <summary>
    /// Imports plugin functions to the kernel using reflection
    /// </summary>
    private void ImportPluginFunctionsToKernel(Kernel kernel, string pluginName, IEnumerable<KernelFunction> functions)
    {
        try
        {
            // Use the modern API directly
            kernel.Plugins.AddFromFunctions(pluginName, functions);
            Logger.LogInformation("Successfully imported plugin functions '{PluginName}' to kernel", pluginName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error importing plugin functions to kernel");
        }
    }

    /// <summary>
    /// Generates a function name for a GAgent event
    /// </summary>
    private string GenerateFunctionName(GrainType grainType, Type eventType)
    {
        const int maxLength = 64;
        // Reserve space for potential plugin prefix
        // Plugin name format: "GA_{grainType}" could add extra length
        const int reservedPrefixLength = 3; // "GA_" 
        const int effectiveMaxLength = maxLength - reservedPrefixLength - 1; // -1 for potential dot separator

        // Clean the grain type string to make it a valid function name
        var cleanGrainType = grainType.ToString()!
            .Replace("/", "_")
            .Replace(".", "_")
            .Replace("-", "_");

        // Clean the event type name
        var cleanEventType = eventType.Name
            .Replace(".", "_")
            .Replace("-", "_");

        // Try just the event type name first (simplest and most readable)
        if (cleanEventType.Length <= effectiveMaxLength)
        {
            Logger.LogInformation(
                "Generated function name: {FunctionName} (length: {Length}) for GrainType: {GrainType}, EventType: {EventType}",
                cleanEventType, cleanEventType.Length, grainType, eventType.Name);
            return cleanEventType;
        }

        // Try with shortened grain type (last part only)
        var parts = grainType.ToString()!.Split('/');
        if (parts.Length > 1)
        {
            cleanGrainType = parts[parts.Length - 1]
                .Replace(".", "_")
                .Replace("-", "_");
        }

        // Try shortened grain type + event type
        var shortGrainType = cleanGrainType.Length > 15 ? cleanGrainType.Substring(0, 15) : cleanGrainType;
        var functionName = $"{shortGrainType}_{cleanEventType}";

        if (functionName.Length <= effectiveMaxLength)
        {
            Logger.LogInformation(
                "Generated function name: {FunctionName} (length: {Length}) for GrainType: {GrainType}, EventType: {EventType}",
                functionName, functionName.Length, grainType, eventType.Name);
            return functionName;
        }

        // If still too long, we need to be more aggressive
        // Calculate a stable hash for uniqueness
        var fullName = $"{grainType}_{eventType.Name}";
        var hash = Math.Abs(fullName.GetHashCode()).ToString("X8");

        // Strategy 1: Try to keep some of event type with hash
        var eventPart = cleanEventType.Length > 20 ? cleanEventType.Substring(0, 20) : cleanEventType;
        functionName = $"{eventPart}_{hash}";

        if (functionName.Length <= effectiveMaxLength)
        {
            Logger.LogInformation(
                "Generated function name: {FunctionName} (length: {Length}) for GrainType: {GrainType}, EventType: {EventType}",
                functionName, functionName.Length, grainType, eventType.Name);
            return functionName;
        }

        // Strategy 2: Further shorten event type
        eventPart = cleanEventType.Length > 10 ? cleanEventType.Substring(0, 10) : cleanEventType;
        functionName = $"{eventPart}_{hash}";

        if (functionName.Length <= effectiveMaxLength)
        {
            Logger.LogInformation(
                "Generated function name: {FunctionName} (length: {Length}) for GrainType: {GrainType}, EventType: {EventType}",
                functionName, functionName.Length, grainType, eventType.Name);
            return functionName;
        }

        // Last resort: just use a prefix and hash
        functionName = $"fn_{hash}";

        Logger.LogInformation(
            "Generated function name: {FunctionName} (length: {Length}) for GrainType: {GrainType}, EventType: {EventType}",
            functionName, functionName.Length, grainType, eventType.Name);

        // This should never exceed effectiveMaxLength (3 + 8 = 11 characters)
        return functionName;
    }

    /// <summary>
    /// Generates a description for a GAgent function
    /// </summary>
    private string GenerateFunctionDescription(GrainType grainType, Type eventType, string gAgentDescription)
    {
        return $"Execute {eventType.Name} on {grainType} GAgent. {gAgentDescription}";
    }

    /// <summary>
    /// Generates a safe function name for MCP tools that won't exceed 64 characters
    /// </summary>
    private string GenerateMCPFunctionName(string serverName, string toolName)
    {
        // For MCP tools, we need to consider the total length including plugin name
        // OpenAI checks the full "plugin.function" name which must be <= 64 chars
        // Plugin name format: MCP_{serverName} (with replacements)
        // So we need to ensure: len("MCP_" + serverName + "." + functionName) <= 64

        const int maxTotalLength = 64;

        // Clean server name for plugin name
        var cleanServerName = serverName
            .Replace("/", "_")
            .Replace(".", "_")
            .Replace("-", "_")
            .Replace(" ", "_");

        // Calculate plugin name and its length
        var pluginName = $"MCP_{cleanServerName}";
        var pluginPrefixLength = pluginName.Length + 1; // +1 for the dot separator

        // Calculate max allowed function name length
        var maxFunctionLength = maxTotalLength - pluginPrefixLength;

        // Clean tool name
        var cleanToolName = toolName
            .Replace("/", "_")
            .Replace(".", "_")
            .Replace("-", "_")
            .Replace(" ", "_");

        // If tool name is already short enough, use it
        if (cleanToolName.Length <= maxFunctionLength)
        {
            Logger.LogDebug("MCP function name: {FunctionName} (length: {Length}, total with plugin: {Total})",
                cleanToolName, cleanToolName.Length, pluginPrefixLength + cleanToolName.Length);
            return cleanToolName;
        }

        // Tool name is too long, we need to shorten it
        var hash = Math.Abs($"{serverName}_{toolName}".GetHashCode()).ToString("X8");

        // Try to keep some meaningful part of the tool name
        var hashLength = hash.Length + 1; // +1 for underscore
        var maxMeaningfulLength = maxFunctionLength - hashLength;

        if (maxMeaningfulLength > 10) // Keep at least 10 chars of the tool name
        {
            var shortenedToolName = cleanToolName.Substring(0, maxMeaningfulLength);
            var shortened = $"{shortenedToolName}_{hash}";
            Logger.LogWarning(
                "MCP tool '{ToolName}' shortened to '{FunctionName}' (plugin.function: {PluginFunction}, length: {Length})",
                toolName, shortened, $"{pluginName}.{shortened}", pluginPrefixLength + shortened.Length);
            return shortened;
        }
        else
        {
            // Very limited space, just use hash with prefix
            var shortened = $"fn_{hash}";
            Logger.LogWarning(
                "MCP tool '{ToolName}' replaced with hash '{FunctionName}' (plugin.function: {PluginFunction}, length: {Length})",
                toolName, shortened, $"{pluginName}.{shortened}", pluginPrefixLength + shortened.Length);
            return shortened;
        }
    }

    /// <summary>
    /// Checks if a GAgent type is allowed based on configuration
    /// </summary>
    private bool IsGAgentAllowed(GrainType grainType)
    {
        if (State.AllowedGAgentTypes.Count == 0)
        {
            // No restrictions, all GAgents are allowed
            return true;
        }

        return State.AllowedGAgentTypes.Contains(grainType);
    }

    /// <summary>
    /// Unregisters all GAgent tools
    /// </summary>
    protected virtual async Task UnregisterGAgentToolsAsync()
    {
        try
        {
            // Clear plugin reference
            _gAgentToolPlugin = null;

            // Update state
            var clearFunctionsEvent = CreateSetRegisteredFunctionsEvent(new List<string>());
            if (clearFunctionsEvent != null)
            {
                RaiseEvent(clearFunctionsEvent);
                await ConfirmEvents();
            }

            Logger.LogInformation("Unregistered all GAgent tools");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unregister GAgent tools");
        }
    }

    /// <summary>
    /// Update kernel with all registered tools
    /// </summary>
    protected async Task UpdateKernelWithAllToolsAsync()
    {
        var kernel = GetKernelFromBrain();
        if (kernel == null)
        {
            Logger.LogWarning("Cannot update kernel tools: Kernel not available");
            return;
        }

        await UpdateKernelWithMCPToolsAsync();

        if (State.EnableGAgentTools && State.SelectedGAgents != null && State.SelectedGAgents.Count > 0)
        {
            // Only register GAgent tools if specific GAgents have been selected
            await UpdateKernelWithGAgentToolsAsync();
        }
    }

    /// <summary>
    /// Represents a GAgent tool registered in the system
    /// </summary>
    protected class GAgentTool
    {
        public GrainType GrainType { get; set; }
        public Type EventType { get; set; }
        public string FunctionName { get; set; }
        public string Description { get; set; }
        public KernelFunction KernelFunction { get; set; }
    }

    /// <summary>
    /// State log event for enabling/disabling GAgent tools
    /// </summary>
    [GenerateSerializer]
    public class SetEnableGAgentToolsStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public bool EnableGAgentTools { get; set; }
    }

    /// <summary>
    /// State log event for setting registered GAgent functions
    /// </summary>
    [GenerateSerializer]
    public class
        SetRegisteredGAgentFunctionsStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<string> RegisteredFunctions { get; set; } = new();
    }

    /// <summary>
    /// State log event for setting allowed GAgent types
    /// </summary>
    [GenerateSerializer]
    public class SetAllowedGAgentTypesStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<GrainType> AllowedGAgentTypes { get; set; } = [];
    }

    /// <summary>
    /// State log event for setting selected GAgent tools
    /// </summary>
    [GenerateSerializer]
    public class SetSelectedGAgentsStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<GrainType> SelectedGAgents { get; set; } = new();
    }

    /// <summary>
    /// Registers all available GAgent tools
    /// </summary>
    public virtual async Task<bool> RegisterAllGAgentToolsAsync()
    {
        try
        {
            if (_brain == null)
            {
                Logger.LogWarning("Cannot register GAgent tools: Brain not initialized");
                return false;
            }

            Logger.LogInformation("Registering all available GAgent tools");

            // Enable GAgent tools
            State.EnableGAgentTools = true;

            // Register all available GAgents
            await RegisterGAgentsAsToolsAsync();

            Logger.LogInformation("Successfully registered all available GAgent tools");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register all GAgent tools");
            return false;
        }
    }

    public Task<List<GrainType>> GetAvailableGAgentToolsAsync()
    {
        return Task.FromResult(State.AllowedGAgentTypes);
    }

    /// <summary>
    /// Configure selected GAgent tools
    /// </summary>
    public virtual async Task<bool> ConfigureGAgentToolsAsync(List<GrainType> selectedGAgents)
    {
        try
        {
            if (_brain == null)
            {
                Logger.LogWarning("Cannot configure GAgent tools: Brain not initialized");
                return false;
            }

            Logger.LogInformation("Configuring GAgent tools: {Count} GAgents selected", selectedGAgents.Count);

            // Enable GAgent tools if not already enabled
            if (!State.EnableGAgentTools)
            {
                RaiseEvent(new SetEnableGAgentToolsStateLogEvent { EnableGAgentTools = true });
            }

            // Update state with selected GAgents
            RaiseEvent(new SetSelectedGAgentsStateLogEvent { SelectedGAgents = selectedGAgents });

            // Persist state changes
            await ConfirmEvents();

            // Update kernel with new tools
            await UpdateKernelWithGAgentToolsAsync();

            Logger.LogInformation("Successfully configured {Count} GAgent tools", selectedGAgents.Count);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure GAgent tools");
            return false;
        }
    }

    /// <summary>
    /// Clears all registered GAgent tools
    /// </summary>
    public virtual async Task<bool> ClearGAgentToolsAsync()
    {
        try
        {
            if (_brain == null)
            {
                Logger.LogWarning("Cannot clear GAgent tools: Brain not initialized");
                return false;
            }

            Logger.LogInformation("Clearing all GAgent tools");

            // Clear selected GAgents
            RaiseEvent(new SetSelectedGAgentsStateLogEvent { SelectedGAgents = [] });

            // Clear registered functions
            RaiseEvent(new SetRegisteredGAgentFunctionsStateLogEvent { RegisteredFunctions = [] });

            // Persist state changes
            await ConfirmEvents();

            // Update kernel to remove tools
            var kernel = GetKernelFromBrain();
            if (kernel != null)
            {
                // Remove all GAgent plugins
                var gagentPlugins = kernel.Plugins.Where(p => p.Name.StartsWith("GA_") || p.Name == "GAgentTools")
                    .ToList();
                foreach (var plugin in gagentPlugins)
                {
                    kernel.Plugins.Remove(plugin);
                }
            }

            Logger.LogInformation("Successfully cleared all GAgent tools");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to clear GAgent tools");
            return false;
        }
    }

    /// <summary>
    /// Update kernel with selected GAgent tools
    /// </summary>
    protected async Task UpdateKernelWithGAgentToolsAsync()
    {
        if (_brain == null || State.SelectedGAgents == null || State.SelectedGAgents.Count == 0)
        {
            Logger.LogInformation("No GAgent tools to register");
            return;
        }

        var kernel = GetKernelFromBrain();
        if (kernel == null)
        {
            Logger.LogWarning("Cannot update GAgent tools: Kernel not available");
            return;
        }

        try
        {
            _gAgentService ??= ServiceProvider.GetRequiredService<IGAgentService>();
            _gAgentExecutor ??= ServiceProvider.GetRequiredService<IGAgentExecutor>();

            // Create GAgent tool plugin if not exists
            _gAgentToolPlugin ??= new GAgentToolPlugin(_gAgentExecutor, _gAgentService, Logger);

            // Import the plugin with its built-in functions
            ImportPluginToKernel(kernel, _gAgentToolPlugin, "GAgentTools");

            // Get event types for selected GAgents
            var allGAgentInfo = await _gAgentService.GetAllAvailableGAgentInformation();
            var selectedGAgentInfo = new Dictionary<GrainType, List<Type>>();

            foreach (var grainType in State.SelectedGAgents)
            {
                try
                {
                    if (allGAgentInfo.TryGetValue(grainType, out var eventTypes) && eventTypes != null &&
                        eventTypes.Count > 0)
                    {
                        selectedGAgentInfo[grainType] = eventTypes;
                    }
                    else
                    {
                        Logger.LogWarning("No event handlers found for GAgent {GrainType}", grainType);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to get event handlers for GAgent {GrainType}", grainType);
                }
            }

            // Register dynamic functions for selected GAgents
            var registeredFunctions = await RegisterDynamicGAgentFunctionsAsync(kernel, selectedGAgentInfo);

            Logger.LogInformation("Successfully registered {Count} GAgent functions as tools",
                registeredFunctions.Count);

            // Store registered function names in state directly
            var functionNames = registeredFunctions.Select(f => f.Name).ToList();
            State.RegisteredGAgentFunctions = functionNames;

            // Persist state changes
            await ConfirmEvents();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update kernel with GAgent tools");
            throw;
        }
    }

    private async Task RegisterMCPToolsAsync(Kernel kernel)
    {
        // Cast state to AIGAgentStateBase to access MCP properties
        var baseState = State as AIGAgentStateBase;
        if (baseState == null || !baseState.EnableMCPTools)
        {
            Logger.LogDebug("MCP tools are disabled");
            return;
        }

        // For now, we need to get MCP servers from somewhere else
        // This is a placeholder - the actual implementation should get servers from configuration
        var mcpServers = new List<string> { "filesystem" }; // Example server

        try
        {
            // Remove existing MCP plugins to avoid duplicates
            var existingMCPPlugins = kernel.Plugins.Where(p => p.Name.StartsWith("MCP_")).ToList();
            foreach (var plugin in existingMCPPlugins)
            {
                kernel.Plugins.Remove(plugin);
            }

            foreach (var serverName in mcpServers)
            {
                try
                {
                    var mcpGAgent = await GetMCPGAgentAsync(serverName);
                    if (mcpGAgent != null)
                    {
                        var toolsDict = await mcpGAgent.GetAvailableToolsAsync();
                        if (toolsDict != null && toolsDict.Any())
                        {
                            var functions = new List<KernelFunction>();

                            foreach (var tool in toolsDict.Values)
                            {
                                var description = !string.IsNullOrEmpty(tool.Description)
                                    ? tool.Description
                                    : $"Tool {tool.Name} from MCP server {serverName}";

                                // Generate a safe function name that won't exceed 64 characters
                                var safeFunctionName = GenerateMCPFunctionName(serverName, tool.Name);
                                Logger.LogInformation("MCP function name: {FunctionName} (length: {Length})",
                                    safeFunctionName, safeFunctionName.Length);

                                // Create the kernel function with tracking wrapper
                                var function = KernelFunctionFactory.CreateFromMethod(
                                    async (KernelArguments args) =>
                                    {
                                        var toolStartTime = DateTime.UtcNow;
                                        var toolCall = new ToolCallDetail
                                        {
                                            ToolName = tool.Name,
                                            ServerName = serverName,
                                            Arguments = args.ToDictionary(),
                                            Timestamp = toolStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff UTC")
                                        };

                                        try
                                        {
                                            // Call the actual MCP tool
                                            var parameters = ConvertKernelArgumentsToJson(args);
                                            var result = await CallMCPToolAsync(serverName, tool.Name, parameters);

                                            toolCall.Result = JsonSerializer.Serialize(result);
                                            toolCall.Success = true;
                                            toolCall.DurationMs =
                                                (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;

                                            // Add to tracking
                                            _currentToolCalls.Add(toolCall);

                                            Logger.LogInformation(
                                                "[MCP Tool Call] {ServerName}.{ToolName} completed in {Duration}ms",
                                                serverName, tool.Name, toolCall.DurationMs);

                                            return result;
                                        }
                                        catch (Exception ex)
                                        {
                                            toolCall.Success = false;
                                            toolCall.Result = $"Error: {ex.Message}";
                                            toolCall.DurationMs =
                                                (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;

                                            // Add to tracking even if failed
                                            _currentToolCalls.Add(toolCall);

                                            Logger.LogError(ex,
                                                "[MCP Tool Call] {ServerName}.{ToolName} failed after {Duration}ms",
                                                serverName, tool.Name, toolCall.DurationMs);
                                            throw;
                                        }
                                    },
                                    safeFunctionName,
                                    description);

                                // Set parameters metadata
                                SetKernelFunctionParametersFromMCPParameters(function, tool.Parameters);
                                functions.Add(function);
                            }

                            // Create plugin for this MCP server
                            // Clean server name to be a valid plugin name (only ASCII letters, digits, and underscores)
                            var pluginName = $"MCP_{serverName.Replace("-", "_").Replace(".", "_").Replace(" ", "_")}";
                            kernel.Plugins.AddFromFunctions(pluginName, functions);

                            Logger.LogInformation("Registered MCP plugin '{PluginName}' with {ToolCount} tools",
                                pluginName, functions.Count);
                        }
                        else
                        {
                            Logger.LogWarning("No tools found for MCP server: {ServerName}", serverName);
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Failed to get MCP GAgent for server: {ServerName}", serverName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to register tools for MCP server: {ServerName}", serverName);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register MCP tools");
        }
    }

    /// <summary>
    /// Converts KernelArguments to a JSON object for MCP tool calls
    /// </summary>
    private static JsonElement ConvertKernelArgumentsToJson(KernelArguments args)
    {
        var dict = new Dictionary<string, object>();
        foreach (var kvp in args)
        {
            dict[kvp.Key] = kvp.Value ?? new object();
        }

        var json = JsonSerializer.Serialize(dict);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    /// <summary>
    /// Sets the parameter metadata for a kernel function based on the input schema
    /// </summary>
    private void SetKernelFunctionParameters(KernelFunction function, JsonElement? inputSchema)
    {
        if (inputSchema == null || inputSchema.Value.ValueKind != JsonValueKind.Object)
            return;

        try
        {
            var parameters = new List<KernelParameterMetadata>();

            if (inputSchema.Value.TryGetProperty("properties", out var properties) &&
                properties.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in properties.EnumerateObject())
                {
                    var paramName = prop.Name;
                    var paramSchema = prop.Value;

                    // Get description
                    string? description = null;
                    if (paramSchema.TryGetProperty("description", out var descJsonElement))
                    {
                        description = descJsonElement.GetString();
                    }

                    // Get type
                    Type paramType = typeof(object);
                    if (paramSchema.TryGetProperty("type", out var typeJsonElement))
                    {
                        var typeStr = typeJsonElement.GetString();
                        if (typeStr != null)
                        {
                            paramType = typeStr switch
                            {
                                "string" => typeof(string),
                                "number" => typeof(double),
                                "integer" => typeof(int),
                                "boolean" => typeof(bool),
                                "array" => typeof(List<object>),
                                "object" => typeof(Dictionary<string, object>),
                                _ => typeof(object)
                            };
                        }
                    }

                    // Check if required
                    bool isRequired = false;
                    if (inputSchema.Value.TryGetProperty("required", out var required) &&
                        required.ValueKind == JsonValueKind.Array)
                    {
                        isRequired = required.EnumerateArray().Any(r => r.GetString() == paramName);
                    }

                    // Create parameter metadata with only the name constructor
                    var metadata = new KernelParameterMetadata(paramName);

                    // Use reflection to set properties to handle API changes
                    var metadataType = metadata.GetType();

                    // Try to set Description property if it exists
                    var descriptionProperty = metadataType.GetProperty("Description");
                    if (descriptionProperty != null && descriptionProperty.CanWrite)
                    {
                        try
                        {
                            descriptionProperty.SetValue(metadata, description);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug(ex, "Could not set Description property on KernelParameterMetadata");
                        }
                    }

                    // Try to set ParameterType property if it exists
                    var typeProperty = metadataType.GetProperty("ParameterType");
                    if (typeProperty != null && typeProperty.CanWrite)
                    {
                        try
                        {
                            typeProperty.SetValue(metadata, paramType);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug(ex, "Could not set ParameterType property on KernelParameterMetadata");
                        }
                    }

                    // Try to set IsRequired property if it exists
                    var reqProp = metadataType.GetProperty("IsRequired");
                    if (reqProp != null && reqProp.CanWrite)
                    {
                        try
                        {
                            reqProp.SetValue(metadata, isRequired);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug(ex, "Could not set IsRequired property on KernelParameterMetadata");
                        }
                    }

                    parameters.Add(metadata);
                }
            }

            // Use reflection to set the parameters
            var metadataProperty =
                typeof(KernelFunction).GetProperty("Metadata", BindingFlags.Public | BindingFlags.Instance);
            if (metadataProperty != null)
            {
                var metadata = metadataProperty.GetValue(function);
                if (metadata != null)
                {
                    var parametersProperty = metadata.GetType()
                        .GetProperty("Parameters", BindingFlags.Public | BindingFlags.Instance);
                    if (parametersProperty != null && parametersProperty.CanWrite)
                    {
                        parametersProperty.SetValue(metadata, parameters.AsReadOnly());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to set kernel function parameters from schema");
        }
    }

    /// <summary>
    /// Sets the parameter metadata for a kernel function based on MCP parameters
    /// </summary>
    private void SetKernelFunctionParametersFromMCPParameters(KernelFunction function,
        Dictionary<string, MCPParameterInfo> mcpParameters)
    {
        try
        {
            var parameters = new List<KernelParameterMetadata>();

            foreach (var kvp in mcpParameters)
            {
                var paramName = kvp.Key;
                var paramInfo = kvp.Value;

                // Get type from MCP parameter info
                Type paramType = typeof(object);
                if (!string.IsNullOrEmpty(paramInfo.Type))
                {
                    paramType = paramInfo.Type switch
                    {
                        "string" => typeof(string),
                        "number" => typeof(double),
                        "integer" => typeof(int),
                        "boolean" => typeof(bool),
                        "array" => typeof(List<object>),
                        "object" => typeof(Dictionary<string, object>),
                        _ => typeof(object)
                    };
                }

                // Create parameter metadata with only the name constructor
                var metadata = new KernelParameterMetadata(paramName);

                // Use reflection to set properties to handle API changes
                var metadataType = metadata.GetType();

                // Try to set Description property if it exists
                var descriptionProperty = metadataType.GetProperty("Description");
                if (descriptionProperty != null && descriptionProperty.CanWrite)
                {
                    try
                    {
                        descriptionProperty.SetValue(metadata, paramInfo.Description);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "Could not set Description property on KernelParameterMetadata");
                    }
                }

                // Try to set ParameterType property if it exists
                var typeProperty = metadataType.GetProperty("ParameterType");
                if (typeProperty != null && typeProperty.CanWrite)
                {
                    try
                    {
                        typeProperty.SetValue(metadata, paramType);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "Could not set ParameterType property on KernelParameterMetadata");
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

            // Use reflection to set the parameters
            var metadataProperty =
                typeof(KernelFunction).GetProperty("Metadata", BindingFlags.Public | BindingFlags.Instance);
            if (metadataProperty != null)
            {
                var metadata = metadataProperty.GetValue(function);
                if (metadata != null)
                {
                    var parametersProperty = metadata.GetType()
                        .GetProperty("Parameters", BindingFlags.Public | BindingFlags.Instance);
                    if (parametersProperty != null && parametersProperty.CanWrite)
                    {
                        parametersProperty.SetValue(metadata, parameters.AsReadOnly());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to set kernel function parameters from MCP parameters");
        }
    }

    /// <summary>
    /// Sets the parameter metadata for a kernel function based on event type properties
    /// </summary>
    private void SetKernelFunctionParametersFromEventType(KernelFunction function, Type eventType)
    {
        try
        {
            var parameters = new List<KernelParameterMetadata>();

            // Get all public properties of the event type
            var properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.Name != "TraceId" && p.Name != "Timestamp");

            foreach (var property in properties)
            {
                // Get description from DescriptionAttribute if available
                var descriptionAttr = property.GetCustomAttribute<DescriptionAttribute>();
                var description = descriptionAttr?.Description ?? $"{property.Name} parameter";

                // Create parameter metadata with only the name constructor
                var metadata = new KernelParameterMetadata(property.Name);

                // Use reflection to set properties to handle API changes
                var metadataType = metadata.GetType();

                // Try to set Description property if it exists
                var descriptionProperty = metadataType.GetProperty("Description");
                if (descriptionProperty != null && descriptionProperty.CanWrite)
                {
                    try
                    {
                        descriptionProperty.SetValue(metadata, description);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "Could not set Description property on KernelParameterMetadata");
                    }
                }

                // Try to set ParameterType property if it exists
                var typeProperty = metadataType.GetProperty("ParameterType");
                if (typeProperty != null && typeProperty.CanWrite)
                {
                    try
                    {
                        typeProperty.SetValue(metadata, property.PropertyType);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "Could not set ParameterType property on KernelParameterMetadata");
                    }
                }

                // Try to set IsRequired property if it exists
                var reqProp = metadataType.GetProperty("IsRequired");
                if (reqProp != null && reqProp.CanWrite)
                {
                    try
                    {
                        reqProp.SetValue(metadata, false); // Make all parameters optional for flexibility
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "Could not set IsRequired property on KernelParameterMetadata");
                    }
                }

                parameters.Add(metadata);
            }

            // Use reflection to set the parameters
            var metadataProperty =
                typeof(KernelFunction).GetProperty("Metadata", BindingFlags.Public | BindingFlags.Instance);
            if (metadataProperty != null)
            {
                var metadata = metadataProperty.GetValue(function);
                if (metadata != null)
                {
                    var parametersProperty = metadata.GetType()
                        .GetProperty("Parameters", BindingFlags.Public | BindingFlags.Instance);
                    if (parametersProperty != null && parametersProperty.CanWrite)
                    {
                        parametersProperty.SetValue(metadata, parameters.AsReadOnly());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to set kernel function parameters from event type");
        }
    }

    /// <summary>
    /// Calls a GAgent tool by executing the event handler
    /// </summary>
    private async Task<object> CallGAgentToolAsync(GrainType grainType, Type eventType, KernelArguments args)
    {
        try
        {
            _gAgentExecutor ??= ServiceProvider.GetRequiredService<IGAgentExecutor>();

            // Create event instance
            var eventInstance = Activator.CreateInstance(eventType);
            if (eventInstance == null)
            {
                throw new InvalidOperationException($"Failed to create instance of event type {eventType.Name}");
            }

            // Map KernelArguments to event properties
            foreach (var kvp in args)
            {
                var property = eventType.GetProperty(kvp.Key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        var value = kvp.Value;
                        if (value != null)
                        {
                            // Convert JsonElement to appropriate type if needed
                            if (value is JsonElement jsonElement)
                            {
                                value = ConvertJsonElementToPropertyType(jsonElement, property.PropertyType);
                            }

                            // Convert value to property type
                            var convertedValue = Convert.ChangeType(value, property.PropertyType);
                            property.SetValue(eventInstance, convertedValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to set property {PropertyName} on event {EventType}",
                            property.Name, eventType.Name);
                    }
                }
            }

            // Cast to EventBase for the executor
            if (eventInstance is not EventBase eventBase)
            {
                throw new InvalidOperationException($"Event type {eventType.Name} does not inherit from EventBase");
            }

            // Execute the event handler
            var response = await _gAgentExecutor.ExecuteGAgentEventHandler(grainType, eventBase);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to call GAgent tool {GrainType}.{EventType}", grainType, eventType.Name);
            throw;
        }
    }

    /// <summary>
    /// Converts a JsonElement to the specified property type
    /// </summary>
    private object? ConvertJsonElementToPropertyType(JsonElement jsonElement, Type targetType)
    {
        if (targetType == typeof(string))
            return jsonElement.GetString();

        if (targetType == typeof(int) || targetType == typeof(int?))
            return jsonElement.TryGetInt32(out var intValue) ? intValue : null;

        if (targetType == typeof(long) || targetType == typeof(long?))
            return jsonElement.TryGetInt64(out var longValue) ? longValue : null;

        if (targetType == typeof(double) || targetType == typeof(double?))
            return jsonElement.TryGetDouble(out var doubleValue) ? doubleValue : null;

        if (targetType == typeof(bool) || targetType == typeof(bool?))
            return jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False
                ? jsonElement.GetBoolean()
                : null;

        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            return jsonElement.TryGetDateTime(out var dateValue) ? dateValue : null;

        if (targetType == typeof(Guid) || targetType == typeof(Guid?))
            return jsonElement.TryGetGuid(out var guidValue) ? guidValue : null;

        // For complex types, deserialize the JSON
        try
        {
            var json = jsonElement.GetRawText();
            return JsonSerializer.Deserialize(json, targetType);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets an MCP GAgent by server name
    /// </summary>
    private async Task<IMCPGAgent?> GetMCPGAgentAsync(string serverName)
    {
        try
        {
            var grainType = GrainType.Create($"mcpgagent/{serverName}");
            var agentId = Guid.NewGuid();
            var grain = GrainFactory.GetGrain<IMCPGAgent>(agentId, grainType.ToString());

            return grain;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get MCP GAgent for server: {ServerName}", serverName);
            return null;
        }
    }

    /// <summary>
    /// Calls an MCP tool
    /// </summary>
    private async Task<object> CallMCPToolAsync(string serverName, string toolName, JsonElement parameters)
    {
        try
        {
            var mcpGAgent = await GetMCPGAgentAsync(serverName);
            if (mcpGAgent == null)
            {
                throw new InvalidOperationException($"Failed to get MCP GAgent for server: {serverName}");
            }

            // Convert JsonElement parameters to dictionary
            var arguments = new Dictionary<string, object>();
            if (parameters.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in parameters.EnumerateObject())
                {
                    arguments[prop.Name] = ConvertJsonElementToBasicType(prop.Value);
                }
            }

            var toolResponse = await mcpGAgent.CallToolAsync(serverName, toolName, arguments);
            var result = toolResponse?.Result;

            // Convert JsonElement to basic types for Orleans serialization
            if (result is JsonElement jsonResult)
            {
                return ConvertJsonElementToBasicType(jsonResult);
            }

            return result ?? new Dictionary<string, object> { ["result"] = "Success" };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to call MCP tool {ServerName}.{ToolName}", serverName, toolName);
            throw;
        }
    }

    /// <summary>
    /// Converts a JsonElement to basic types for Orleans serialization
    /// </summary>
    private object ConvertJsonElementToBasicType(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                return element.GetDouble();
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
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = ConvertJsonElementToBasicType(property.Value);
                }

                return dict;
            default:
                return element.ToString();
        }
    }
}