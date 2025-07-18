using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Core;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.MCP.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.GroupChat;

/// <summary>
/// Workflow-aware AIGAgent base class that can automatically discover and utilize resources in the workflow
/// </summary>
public abstract class WorkflowAwareAIGAgentBase<TState, TStateLogEvent, TEvent> :
    AIGAgentBase<TState, TStateLogEvent, TEvent, AIGAgentConfigurationBase>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
{
    /// <summary>
    /// Called when the workflow context is ready
    /// </summary>
    protected virtual async Task OnWorkflowContextReadyAsync(WorkflowExecutionContext context)
    {
        Logger.LogInformation($"Workflow context ready with {context.AvailableResources.Count} resources");

        // Discover MCP providers
        var mcpProviders = await DiscoverMCPProvidersAsync(context.AvailableResources);

        if (mcpProviders.Any())
        {
            Logger.LogInformation($"Found {mcpProviders.Count} MCP providers, configuring tools...");
            await ConfigureMCPProvidersAsync(mcpProviders);
        }

        // Subclasses can handle other types of resources
        await HandleOtherResourcesAsync(context);
    }

    /// <summary>
    /// Discover MCP providers
    /// </summary>
    private async Task<List<IMCPGAgent>> DiscoverMCPProvidersAsync(List<GrainId> resources)
    {
        var mcpProviders = new List<IMCPGAgent>();
        var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();

        foreach (var resourceId in resources)
        {
            try
            {
                var gAgent = await gAgentFactory.GetGAgentAsync(resourceId);
                if (gAgent is IMCPGAgent mcpGAgent)
                {
                    mcpProviders.Add(mcpGAgent);
                    Logger.LogInformation($"Discovered MCP provider: {resourceId}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Failed to check if {resourceId} is MCP provider");
            }
        }

        return mcpProviders;
    }

    /// <summary>
    /// Configure MCP providers
    /// </summary>
    private async Task ConfigureMCPProvidersAsync(List<IMCPGAgent> mcpProviders)
    {
        try
        {
            // Directly use ConfigureMCPServersAsync method, which accepts List<IMCPGAgent>
            var result = await ConfigureMCPServersAsync(mcpProviders);

            if (result)
            {
                Logger.LogInformation($"Successfully configured {mcpProviders.Count} MCP providers");
            }
            else
            {
                Logger.LogWarning("Failed to configure MCP providers");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error configuring MCP providers");
        }
    }

    /// <summary>
    /// Handle other types of resources (subclasses can override)
    /// </summary>
    protected virtual Task HandleOtherResourcesAsync(WorkflowExecutionContext context)
    {
        return Task.CompletedTask;
    }
}