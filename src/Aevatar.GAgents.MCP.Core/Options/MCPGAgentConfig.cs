using GroupChat.GAgent.Dto;

namespace Aevatar.GAgents.MCP.Options;

[GenerateSerializer]
public class MCPGAgentConfig : MemberConfigDto
{
    [Id(0)] public MCPServerConfig Server { get; set; } = new();
    [Id(1)] public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    [Id(2)] public bool EnableToolDiscovery { get; set; } = true;
}