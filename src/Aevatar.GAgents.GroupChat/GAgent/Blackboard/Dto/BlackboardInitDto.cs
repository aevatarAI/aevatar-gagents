using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Feature.Blackboard.Dto;

public class BlackboardInitDto:InitializationEventBase
{
    public  string Topic { get; set; }
}