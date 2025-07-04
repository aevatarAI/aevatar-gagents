using Orleans;

namespace Aevatar.GAgents.MCP.Model;

[GenerateSerializer]
public class MCPParameterInfo
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty;
    [Id(2)] public string Description { get; set; } = string.Empty;
    [Id(3)] public bool Required { get; set; }
    [Id(4)] public object? DefaultValue { get; set; }
}
