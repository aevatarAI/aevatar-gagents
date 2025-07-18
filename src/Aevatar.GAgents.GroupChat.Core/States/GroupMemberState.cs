using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.GroupChat.Core;

namespace GroupChat.GAgent.GEvent;

[GenerateSerializer]
public class GroupMemberState : AIGAgentStateBase
{
    [Id(0)] public string MemberName { get; set; }
    [Id(1)] public WorkflowExecutionContext WorkflowContext { get; set; }
}