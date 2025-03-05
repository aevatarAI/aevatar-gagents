using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class InitializeDto
{
    [Id(0)]
    public string Instructions { get; set; }
    
    [Required]
    [Id(1)] public LLMConfigDto LLMConfig { get; set; }
}