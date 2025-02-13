using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Feature.Coordinator.LogEvent;

[GenerateSerializer]
public class CoordinatorLogEventBase:StateLogEventBase<CoordinatorLogEventBase>
{
    
}

[GenerateSerializer]
public class AddChatTermLogEvent : CoordinatorLogEventBase
{
    
}

[GenerateSerializer]
public class TriggerCoordinator : CoordinatorLogEventBase
{
}