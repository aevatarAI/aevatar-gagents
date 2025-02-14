using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.SEvent;

[GenerateSerializer]
public class GroupMemberLogEvent:StateLogEventBase<GroupMemberLogEvent>
{
    
}

[GenerateSerializer]
public class SetMemberNameLogEvent:GroupMemberLogEvent
{
    [Id(0)] public string MemberName { get; set; }
}