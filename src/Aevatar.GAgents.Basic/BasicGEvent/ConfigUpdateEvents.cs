using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.BasicGEvent;

[GenerateSerializer]
public class ConfigUpdateEvent : EventWithResponseBase<ConfigResponseEvent>
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty; // e.g., "SystemLLMConfigs", "MCPServerOptions"
    [Id(1)] public string ConfigJson { get; set; } = string.Empty; // JSON serialized configuration
}

[GenerateSerializer]
public class ConfigRequestEvent : EventWithResponseBase<ConfigResponseEvent>
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty; // e.g., "SystemLLMConfigs", "MCPServerOptions"
    [Id(1)] public string? ConfigKey { get; set; } // Optional: specific key within the config
}

[GenerateSerializer]
public class ConfigResponseEvent : EventBase
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty;
    [Id(1)] public string ConfigJson { get; set; } = string.Empty;
    [Id(2)] public bool Success { get; set; }
    [Id(3)] public string? ErrorMessage { get; set; }
}