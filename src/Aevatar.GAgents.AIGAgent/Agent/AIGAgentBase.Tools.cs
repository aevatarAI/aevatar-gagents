using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AIGAgent.GEvents;
using Aevatar.GAgents.AIGAgent.Plugin;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Executor;
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
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    private IGAgentService? _gAgentService;
    private IGAgentExecutor? _gAgentExecutor;
    private GAgentToolPlugin? _gAgentToolPlugin;

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

            // Store registered function names in state using the correct event type
            var functionNames = registeredFunctions.Select(f => f.Name).ToList();

            // Create a custom event that inherits from TStateLogEvent
            var setFunctionsEvent = CreateSetRegisteredFunctionsEvent(functionNames);
            if (setFunctionsEvent != null)
            {
                RaiseEvent(setFunctionsEvent);
                await ConfirmEvents();
            }
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

        foreach (var (grainType, eventTypes) in allGAgents)
        {
            try
            {
                // Skip if this is a restricted GAgent type
                if (!IsGAgentAllowed(grainType))
                {
                    Logger.LogDebug("Skipping restricted GAgent type: {GrainType}", grainType);
                    continue;
                }

                var detailInfo = await _gAgentService!.GetGAgentDetailInfoAsync(grainType);

                foreach (var eventType in eventTypes)
                {
                    var functionName = GenerateFunctionName(grainType, eventType);

                    var description = GenerateFunctionDescription(grainType, eventType, detailInfo.Description);

                    // Create a wrapper function that calls the plugin
                    var function = KernelFunctionFactory.CreateFromMethod(
                        method: async (string parameters) =>
                        {
                            return await _gAgentToolPlugin!.InvokeGAgentAsync(
                                grainType.ToString(),
                                eventType.Name,
                                parameters);
                        },
                        functionName: functionName,
                        description: description,
                        parameters: new[]
                        {
                            new KernelParameterMetadata("parameters")
                            {
                                Description =
                                    $"JSON serialized parameters for {eventType.Name}. Check the event type definition for required fields.",
                                IsRequired = true,
                                ParameterType = typeof(string)
                            }
                        },
                        returnParameter: new KernelReturnParameterMetadata
                        {
                            Description = "JSON result containing success status and response data",
                            ParameterType = typeof(string)
                        });

                    dynamicFunctions.Add(function);

                    Logger.LogDebug("Registered function: {FunctionName}", functionName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to register functions for GAgent {GrainType}", grainType);
            }
        }

        // Import all dynamic functions as a plugin
        if (dynamicFunctions.Any())
        {
            ImportPluginFunctionsToKernel(kernel, "DynamicGAgentFunctions", dynamicFunctions);
        }

        return dynamicFunctions;
    }

    /// <summary>
    /// Gets the Semantic Kernel from the brain using reflection
    /// </summary>
    private Kernel? GetKernelFromBrain()
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
            var importMethod = kernel.GetType().GetMethod("ImportPluginFromObject");
            if (importMethod != null)
            {
                importMethod.Invoke(kernel, new[] { plugin, pluginName });
            }
            else
            {
                Logger.LogWarning("Cannot find ImportPluginFromObject method on Kernel");
            }
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
            var importMethod = kernel.GetType().GetMethod("ImportPluginFromFunctions");
            if (importMethod != null)
            {
                importMethod.Invoke(kernel, new object[] { pluginName, functions });
            }
            else
            {
                Logger.LogWarning("Cannot find ImportPluginFromFunctions method on Kernel");
            }
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
        // Clean the grain type string to make it a valid function name
        var cleanGrainType = grainType.ToString()
            .Replace("/", "_")
            .Replace(".", "_")
            .Replace("-", "_");

        return $"{cleanGrainType}_{eventType.Name}";
    }

    /// <summary>
    /// Generates a description for a GAgent function
    /// </summary>
    private string GenerateFunctionDescription(GrainType grainType, Type eventType, string gAgentDescription)
    {
        return $"Execute {eventType.Name} on {grainType} GAgent. {gAgentDescription}";
    }

    /// <summary>
    /// Checks if a GAgent type is allowed based on configuration
    /// </summary>
    private bool IsGAgentAllowed(GrainType grainType)
    {
        if (State.AllowedGAgentTypes == null || State.AllowedGAgentTypes.Count == 0)
        {
            // No restrictions, all GAgents are allowed
            return true;
        }

        var grainTypeString = grainType.ToString();
        return State.AllowedGAgentTypes.Any(allowed =>
            grainTypeString.Contains(allowed, StringComparison.OrdinalIgnoreCase));
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
        [Id(0)] public List<string>? AllowedGAgentTypes { get; set; }
    }
}