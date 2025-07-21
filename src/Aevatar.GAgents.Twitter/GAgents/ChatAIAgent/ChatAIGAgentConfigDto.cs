using GroupChat.GAgent.Dto;
using Orleans;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[GenerateSerializer]
public class ChatAIGAgentConfigDto : GroupMemberConfigDto
{
    [Id(0)] 
    [DefaultValues(
        "You are a helpful AI assistant"
    )]
    public string Instructions { get; set; } = string.Empty;

    [Id(1)] 
    [DefaultValues(
        "gpt-4"
    )]
    public string SystemLLM { get; set; }
}