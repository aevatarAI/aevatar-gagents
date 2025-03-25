using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.SEvent;
using Orleans.EventSourcing;

namespace GroupChat.GAgent.Feature.Blackboard.LogEvent;

[GenerateSerializer]
public class BlackboardLogEvent : StateLogEventBase<BlackboardLogEvent>
{
}

[GenerateSerializer]
public class AddChatHistoryLogEvent : BlackboardLogEvent
{
    [Id(0)] public MessageType MessageType { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string AgentName { get; set; }
    [Id(3)] public string Content { get; set; }
}