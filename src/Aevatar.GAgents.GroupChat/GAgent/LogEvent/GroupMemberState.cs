using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.GEvent;

[GenerateSerializer]
public class GroupMemberState:StateBase
{
    [Id(0)] public string MemberName { get; set; }
}