using GroupChat.GAgent.Dto;
using Orleans;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[GenerateSerializer]
public class ChatAIGAgentConfigDto : GroupMemberConfigDto
{
    [Id(0)]
    public string Instructions { get; set; } = string.Empty;
    
    [Id(1)]
    public LLMConfigDto LLMConfig { get; set; } = new();
} 