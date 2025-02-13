using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Feature.Coordinator.LogEvent;

[GenerateSerializer]
public class CoordinatorStateBase : StateBase
{
    [Id(1)] public int ChatTerm { get; set; } = 0;
    [Id(2)] public bool IfTriggerCoordinate = false;

    public void Apply(AddChatTermLogEvent @event)
    {
        ChatTerm += 1;
        IfTriggerCoordinate = false;
    }

    public void Apply(TriggerCoordinator @event)
    {
        IfTriggerCoordinate = true;
    }
}

[GenerateSerializer]
public class InterestInfo
{
    [Id(0)] public Guid MemberId { get; set; }
    [Id(2)] public int InterestValue { get; set; }
}

[GenerateSerializer]
public class GroupMember
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string  Name { get; set; }
    [Id(2)] public DateTime UpdateTime { get; set; }
}