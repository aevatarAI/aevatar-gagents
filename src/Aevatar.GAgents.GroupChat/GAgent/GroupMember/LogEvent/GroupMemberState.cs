using Aevatar.GAgents.AIGAgent.State;

namespace GroupChat.GAgent.GEvent;

[GenerateSerializer]
public class GroupMemberState : AIGAgentStateBase
{
    [Id(0)] public string MemberName { get; set; }
}