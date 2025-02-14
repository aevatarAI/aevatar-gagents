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
public class SpeechEvent:EventFromCoordinatorBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid Speaker { get; set; }
}

[GenerateSerializer]
public class SpeechEventForCoordinator : SpeechEvent
{
    
}

[GenerateSerializer]
public class SpeechResponseEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string MemberName { get; set; }
    [Id(3)] public TalkResponse TalkResponse { get; set; }
}