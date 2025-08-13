using System.ComponentModel;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.GroupChat.Core.Dto;

namespace Aevatar.GAgents.PsiOmni;

[GenerateSerializer]
public class PsiOmniGAgentConfig : GroupMemberConfigDto
{
    [Id(0)] 
    [Description("The depth level for PsiOmni agent's reasoning and processing capabilities")]
    public int Depth { get; set; } = 0;
    
    [Id(1)] 
    [Description("Optional LLM configuration for PsiOmni agent's AI capabilities")]
    public LLMConfigDto? LLMConfig { get; set; }
}