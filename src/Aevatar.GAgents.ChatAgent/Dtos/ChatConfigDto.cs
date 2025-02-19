using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.ChatAgent.Dtos;

[GenerateSerializer]
public class ChatConfigDto: ConfigurationBase
{
    [Id(0)]
    public string Instructions { get; set; }
    [Id(1)]
    public string LLM { get; set; }
    [Id(2)] public int MaxHistoryCount { get; set; } = 20;
}