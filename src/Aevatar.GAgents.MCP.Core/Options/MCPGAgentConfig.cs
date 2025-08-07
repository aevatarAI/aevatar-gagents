using System.ComponentModel.DataAnnotations;
using GroupChat.GAgent.Dto;

namespace Aevatar.GAgents.MCP.Options;

[GenerateSerializer]
public class MCPGAgentConfig : MemberConfigDto
{
    [Id(0)] 
    [Required(ErrorMessage = "Server Configuration is required")]
    public MCPServerConfig ServerConfig { get; set; } = new();
    
    [Id(1)] 
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}