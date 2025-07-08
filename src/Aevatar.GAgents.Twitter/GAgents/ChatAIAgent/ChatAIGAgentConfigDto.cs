using GroupChat.GAgent.Dto;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[GenerateSerializer]
public class ChatAIGAgentConfigDto : GroupMemberConfigDto
{
    // No additional chat-specific configuration needed
    // LLM model and other AI settings are handled by InitializeAsync
} 