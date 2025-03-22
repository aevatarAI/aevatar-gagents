using System.ComponentModel.DataAnnotations;
using Aevatar.GAgents.AI.Options;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class InitializeDto
{
    [Id(0)]
    public string Instructions { get; set; }
    
    [Required]
    [Id(1)] public LLMConfigDto LLMConfig { get; set; }
    [Id(2)] public bool StreamingModeEnabled { get; set; }
    [Id(3)] public StreamingConfig StreamingConfig { get; set; }
}