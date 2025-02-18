using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Feature.Coordinator.LogEvent;

[GenerateSerializer]
public class CoordinatorLogEventBase:StateLogEventBase<CoordinatorLogEventBase>
{
    
}

[GenerateSerializer]
public class AddChatTermLogEvent : CoordinatorLogEventBase
{
    [Id(0)] public bool IfComplete { get; set; }
}

[GenerateSerializer]
public class TriggerCoordinator : CoordinatorLogEventBase
{
    [Id(0)] public Guid MemberId { get; set; }
    [Id(1)] public DateTime CreateTime { get; set; }
}
