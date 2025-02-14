using Aevatar.Core.Abstractions;
using GroupChat.GAgent.SEvent;

namespace GroupChat.GAgent.GEvent;

[GenerateSerializer]
public class GroupMemberState:StateBase
{
    [Id(0)] public string MemberName { get; set; }

    public void Apply(SetMemberNameLogEvent @event)
    {
        MemberName = @event.MemberName;
    }
}