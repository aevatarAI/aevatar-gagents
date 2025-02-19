using Orleans;

namespace Aevatar.GAgents.AI.Common;

[GenerateSerializer]
public class ChatMessage
{
    [Id(0)]
    public ChatRole ChatRole { get; set; }
    
    [Id(1)]
    public string? Content { get; set; }
}
