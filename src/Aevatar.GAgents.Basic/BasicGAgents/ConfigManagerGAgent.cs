using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGEvent;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Orleans.Concurrency;

namespace Aevatar.GAgents.Basic.BasicGAgents;

/// <summary>
/// State for ConfigManagerGAgent
/// </summary>
[GenerateSerializer]
public class ConfigManagerGAgentState : StateBase
{
    [Id(0)] public DateTime LastUpdated { get; set; }
    [Id(1)] public Dictionary<string, DateTime> ConfigUpdateTimes { get; set; } = new();
    [Id(2)] public int TotalUpdates { get; set; }
    [Id(3)] public string ConfigJson { get; set; } = string.Empty;
    [Id(4)] public string ConfigType { get; set; } = string.Empty;
}

/// <summary>
/// State log events for ConfigManagerGAgent
/// </summary>
[GenerateSerializer]
public class ConfigManagerStateLogEvent : StateLogEventBase<ConfigManagerStateLogEvent>;

[GenerateSerializer]
public class ConfigUpdatedLogEvent : ConfigManagerStateLogEvent
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty;
    [Id(1)] public bool Success { get; set; }
    [Id(2)] public string? ErrorMessage { get; set; }
    [Id(3)] public DateTime Timestamp { get; set; }
    [Id(4)] public string ConfigJson { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ConfigSetLogEvent : ConfigManagerStateLogEvent
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty;
    [Id(1)] public string ConfigJson { get; set; } = string.Empty;
    [Id(2)] public DateTime Timestamp { get; set; }
}

public interface IConfigManagerGAgent : IStateGAgent<ConfigManagerGAgentState>
{
    Task<ConfigResponseEvent> UpdateConfigAsync(ConfigUpdateEvent updateEvent);
    [ReadOnly]
    Task<ConfigResponseEvent> RequestConfigAsync(ConfigRequestEvent requestEvent);
}

/// <summary>
/// GAgent responsible for managing configuration updates
/// Each instance stores one type of Options configuration
/// Primary key is generated from Options type's FullName
/// </summary>
[GAgent("config", "aevatar")]
public class ConfigManagerGAgent : GAgentBase<ConfigManagerGAgentState, ConfigManagerStateLogEvent>,
    IConfigManagerGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        var configType = string.IsNullOrEmpty(State.ConfigType) ? "Not configured" : State.ConfigType;
        return Task.FromResult($"Configuration manager GAgent for type: {configType}. " +
                               $"Last updated: {State.LastUpdated:yyyy-MM-dd HH:mm:ss}, " +
                               $"Total updates: {State.TotalUpdates}");
    }

    /// <summary>
    /// Update configuration
    /// </summary>
    public async Task<ConfigResponseEvent> UpdateConfigAsync(ConfigUpdateEvent updateEvent)
    {
        return await HandleEventAsync(updateEvent);
    }

    /// <summary>
    /// Request configuration
    /// </summary>
    public async Task<ConfigResponseEvent> RequestConfigAsync(ConfigRequestEvent requestEvent)
    {
        return await HandleEventAsync(requestEvent);
    }

    /// <summary>
    /// Handle configuration update events
    /// </summary>
    [EventHandler]
    public async Task<ConfigResponseEvent> HandleEventAsync(ConfigUpdateEvent updateEvent)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(updateEvent.ConfigType))
            {
                throw new ArgumentException("ConfigType cannot be empty");
            }

            if (string.IsNullOrEmpty(updateEvent.ConfigJson))
            {
                throw new ArgumentException("ConfigJson cannot be empty");
            }

            // Validate JSON format
            try
            {
                JsonDocument.Parse(updateEvent.ConfigJson);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}", ex);
            }

            // Raise state update event
            RaiseEvent(new ConfigSetLogEvent
            {
                ConfigType = updateEvent.ConfigType,
                ConfigJson = updateEvent.ConfigJson,
                Timestamp = DateTime.UtcNow
            });

            await ConfirmEvents();

            // Log success
            RaiseEvent(new ConfigUpdatedLogEvent
            {
                ConfigType = updateEvent.ConfigType,
                Success = true,
                Timestamp = DateTime.UtcNow,
                ConfigJson = updateEvent.ConfigJson
            });

            await ConfirmEvents();

            Logger.LogInformation("Successfully updated configuration for type: {ConfigType}\n{ConfigJson}",
                updateEvent.ConfigType, updateEvent.ConfigJson);

            return new ConfigResponseEvent
            {
                ConfigType = updateEvent.ConfigType,
                ConfigJson = updateEvent.ConfigJson,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update configuration for type: {ConfigType}", updateEvent.ConfigType);

            // Log failure
            RaiseEvent(new ConfigUpdatedLogEvent
            {
                ConfigType = updateEvent.ConfigType,
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow,
                ConfigJson = string.Empty
            });

            await ConfirmEvents();

            return new ConfigResponseEvent
            {
                ConfigType = updateEvent.ConfigType,
                ConfigJson = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Handle configuration get events
    /// </summary>
    [EventHandler]
    public async Task<ConfigResponseEvent> HandleEventAsync(ConfigRequestEvent requestEvent)
    {
        try
        {
            // Check if we have configuration stored
            if (string.IsNullOrEmpty(State.ConfigJson))
            {
                return new ConfigResponseEvent
                {
                    ConfigType = requestEvent.ConfigType,
                    ConfigJson = string.Empty,
                    Success = false,
                    ErrorMessage = "No configuration found"
                };
            }

            // Check if config type matches
            if (!string.IsNullOrEmpty(State.ConfigType) &&
                State.ConfigType != requestEvent.ConfigType)
            {
                return new ConfigResponseEvent
                {
                    ConfigType = requestEvent.ConfigType,
                    ConfigJson = string.Empty,
                    Success = false,
                    ErrorMessage =
                        $"Configuration type mismatch. Expected: {State.ConfigType}, Requested: {requestEvent.ConfigType}"
                };
            }

            // If a specific key is requested, extract it from the JSON
            if (!string.IsNullOrEmpty(requestEvent.ConfigKey))
            {
                try
                {
                    var configObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(State.ConfigJson);
                    if (configObject != null && configObject.TryGetValue(requestEvent.ConfigKey, out var value))
                    {
                        var valueJson = JsonSerializer.Serialize(value);
                        Logger.LogInformation($"Successfully extracted configuration key: {requestEvent.ConfigKey}");
                        return new ConfigResponseEvent
                        {
                            ConfigType = requestEvent.ConfigType,
                            ConfigJson = valueJson,
                            Success = true
                        };
                    }

                    return new ConfigResponseEvent
                    {
                        ConfigType = requestEvent.ConfigType,
                        ConfigJson = string.Empty,
                        Success = false,
                        ErrorMessage = $"Configuration key '{requestEvent.ConfigKey}' not found"
                    };
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to extract configuration key: {ConfigKey}", requestEvent.ConfigKey);
                    // If extraction fails, return the whole config
                }
            }

            return new ConfigResponseEvent
            {
                ConfigType = State.ConfigType,
                ConfigJson = State.ConfigJson,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve configuration for type: {ConfigType}", requestEvent.ConfigType);

            return new ConfigResponseEvent
            {
                ConfigType = requestEvent.ConfigType,
                ConfigJson = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    protected override void GAgentTransitionState(ConfigManagerGAgentState state,
        StateLogEventBase<ConfigManagerStateLogEvent> @event)
    {
        switch (@event)
        {
            case ConfigSetLogEvent setEvent:
                state.ConfigType = setEvent.ConfigType;
                state.ConfigJson = setEvent.ConfigJson;
                state.LastUpdated = setEvent.Timestamp;
                break;

            case ConfigUpdatedLogEvent updateEvent:
                state.LastUpdated = updateEvent.Timestamp;
                state.ConfigUpdateTimes[updateEvent.ConfigType] = updateEvent.Timestamp;
                if (updateEvent.Success)
                {
                    state.TotalUpdates++;
                }

                break;
        }
    }
}