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
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00", ErrorMessage = "Request Timeout must be between 1 second and 10 minutes")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}