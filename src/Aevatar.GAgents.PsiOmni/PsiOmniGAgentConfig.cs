using GroupChat.GAgent.Dto;

namespace Aevatar.GAgents.PsiOmni;

[GenerateSerializer]
public class PsiOmniGAgentConfig : GroupMemberConfigDto
{
    [Id(0)] public int Depth { get; set; } = 0;
}