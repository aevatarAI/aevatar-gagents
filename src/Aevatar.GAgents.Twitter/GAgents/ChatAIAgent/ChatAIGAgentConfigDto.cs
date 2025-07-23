using GroupChat.GAgent.Dto;
using Orleans;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[GenerateSerializer]
public class ChatAIGAgentConfigDto : GroupMemberConfigDto
{
    [Id(0)] 
    public string Instructions { get; set; } = "You are a helpful AI assistant";

    [Id(1)] 
    public string SystemLLM { get; set; } = "gpt-4";
}