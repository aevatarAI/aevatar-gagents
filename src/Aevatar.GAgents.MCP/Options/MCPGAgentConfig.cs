using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.MCP.Options;

[GenerateSerializer]
public class MCPGAgentConfig : ConfigurationBase
{
    [Id(0)] public MCPServerConfig? Server { get; set; }
    [Id(1)] public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    [Id(2)] public bool EnableToolDiscovery { get; set; } = true;
}