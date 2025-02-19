using System.Collections.Generic;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class InitializeDto
{
    [Id(0)]
    public string Instructions { get; set; }
    [Id(1)]
    public string LLM { get; set; }
}