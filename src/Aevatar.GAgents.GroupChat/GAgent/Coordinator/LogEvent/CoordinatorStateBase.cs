using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Feature.Coordinator.LogEvent;

[GenerateSerializer]
public class CoordinatorStateBase : StateBase
{
    [Id(1)] public int ChatTerm { get; set; } = 0;
    [Id(2)] public bool IfTriggerCoordinate { get; set; } = false;
    [Id(3)] public bool IfComplete { get; set; } = false;
    [Id(4)] public Guid CoordinatorSpeaker { get; set; }
    [Id(5)] public DateTime CoordinatorTime { get; set; }

    public void Apply(AddChatTermLogEvent @event)
    {
        ChatTerm += 1;
        IfTriggerCoordinate = false;
        IfComplete = @event.IfComplete;
    }

    public void Apply(TriggerCoordinator @event)
    {
        IfTriggerCoordinate = true;
        CoordinatorSpeaker = @event.MemberId;
        CoordinatorTime = @event.CreateTime;
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