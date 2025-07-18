using System.ComponentModel;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.TypeTestAgent.Dtos;
using Aevatar.GAgents.TypeTestAgent.Events;
using Aevatar.GAgents.TypeTestAgent.State;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.GAgents.TypeTestAgent.Agent;

[Description("Comprehensive type demonstration agent for frontend testing")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(TypeTestAgent))]
public class TypeTestAgent : AIGAgentBase<TypeTestAgentState, TypeTestStateLogEvent, EventBase, TypeTestConfigDto>, ITypeTestAgent
{
    private readonly ILogger<TypeTestAgent> _logger;
    private TypeTestConfigDto _currentConfig;

    public TypeTestAgent(ILogger<TypeTestAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "TypeTest Agent provides comprehensive testing capabilities for all C# primitive types and complex objects. " +
            "It demonstrates serialization, deserialization, and validation of various data types for frontend integration testing."
        );
    }

    public async Task<bool> ApplyTypeTestConfigAsync(TypeTestConfigDto config)
    {
        try
        {
            _currentConfig = config;
            State.LastConfigUpdate = DateTime.UtcNow;
            State.IsInitialized = true;
            
            var configEvent = new TypeTestStateLogEvent
            {
                EventType = "ConfigurationApplied",
                Description = "TypeTest configuration applied successfully",
                Timestamp = DateTime.UtcNow
            };
            
            RaiseEvent(configEvent);
            await ConfirmEvents();
            
            _logger.LogInformation("TypeTest configuration applied successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply TypeTest configuration");
            return false;
        }
    }

    protected override async Task PerformConfigAsync(TypeTestConfigDto initializationConfig)
    {
        await ApplyTypeTestConfigAsync(initializationConfig);
    }

    public async Task<string> GetTypeTestConfigJsonAsync()
    {
        try
        {
            var config = _currentConfig ?? new TypeTestConfigDto();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(config, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert configuration to JSON");
            return "{}";
        }
    }
} 