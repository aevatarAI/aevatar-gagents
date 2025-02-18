using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Common;

namespace GroupChat.GAgent.Feature.Coordinator.GEvent;

[GenerateSerializer]
public class EventFromCoordinatorBase : EventBase
{
    
}

[GenerateSerializer]
public class GroupChatFinishEvent:EventFromCoordinatorBase
{
    [Id(0)]
    public Guid BlackboardId { get; set; }
}


[GenerateSerializer]
public class GroupChatFinishEventForCoordinator:GroupChatFinishEvent
{
    
}

[GenerateSerializer]
public class EvaluationInterestEvent:EventFromCoordinatorBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public int ChatTerm { get; set; }
}

[GenerateSerializer]
public class EvaluationInterestEventForCoordinator : EvaluationInterestEvent
{
    
}

[GenerateSerializer]
public class EvaluationInterestResponseEvent : EventBase
{
    [Id(0)] public Guid MemberId { get; set; }
    [Id(1)] public Guid BlackboardId { get; set; }
    [Id(2)] public int InterestValue { get; set; }
    [Id(3)] public int ChatTerm { get; set; }
}

[GenerateSerializer]
public class CoordinatorPingEvent : EventFromCoordinatorBase
{
    [Id(0)] public Guid BlackboardId { get; set; } 
}

[GenerateSerializer]
public class CoordinatorPingEventForCoordinator : CoordinatorPingEvent
{
    
}

[GenerateSerializer]
public class CoordinatorPongEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string MemberName { get; set; }
}


[GenerateSerializer]
public class ChatEvent:EventFromCoordinatorBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid Speaker { get; set; }
    [Id(3)] public int Term { get; set; }
}

[GenerateSerializer]
public class ChatEventForCoordinator : ChatEvent
{
    
}

[GenerateSerializer]
public class ChatResponseEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string MemberName { get; set; }
    [Id(3)] public ChatResponse ChatResponse { get; set; }
    [Id(4)] public int Term { get; set; }
}