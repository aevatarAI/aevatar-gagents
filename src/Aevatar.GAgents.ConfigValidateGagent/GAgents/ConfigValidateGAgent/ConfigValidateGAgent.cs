using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.GAgents.ConfigValidateGagent.GAgents.ConfigValidateGAgent;

[Description("Configuration Validation GAgent - demonstrates config validation with DataAnnotations and IValidatableObject")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(ConfigValidateGAgent))]
public class ConfigValidateGAgent : 
    GAgentBase<ConfigValidateGAgentState, ConfigValidateGAgentEvent, EventBase, ConfigValidateGAgentConfig>, 
    IConfigValidateGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Configuration Validation GAgent - demonstrates config validation capabilities");
    }

    protected override async Task PerformConfigAsync(ConfigValidateGAgentConfig configuration)
    {
        Logger.LogInformation("Configuring ConfigValidateGAgent, Input Type: {InputType}, Input Content: {InputContent}", 
            configuration.InputType, configuration.InputContent);

        // Simply record configuration information
        RaiseEvent(new ConfigurationUpdatedEvent
        {
            UpdateTime = DateTime.UtcNow
        });

        await ConfirmEvents();

        Logger.LogInformation("ConfigValidateGAgent configuration completed");
    }

    protected override void GAgentTransitionState(ConfigValidateGAgentState state, StateLogEventBase<ConfigValidateGAgentEvent> @event)
    {
        switch (@event)
        {
            case ConfigurationUpdatedEvent configUpdatedEvent:
                state.Id = Guid.NewGuid().ToString();
                state.LastConfigurationUpdate = configUpdatedEvent.UpdateTime;
                break;
        }
    }

    public Task<string> GetValidationStatusAsync()
    {
        return Task.FromResult($"Last updated: {State.LastConfigurationUpdate}");
    }
}