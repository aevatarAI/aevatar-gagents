using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Feature.Coordinator.LogEvent;

[GenerateSerializer]
public class CoordinatorLogEventBase:StateLogEventBase<CoordinatorLogEventBase>
{
    
}

[GenerateSerializer]
public class SetBlackboardLogEvent : CoordinatorLogEventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
}

[GenerateSerializer]
public class AddChatTermLogEvent : CoordinatorLogEventBase
{
    
}

[GenerateSerializer]
public class TriggerCoordinator : CoordinatorLogEventBase
{
}