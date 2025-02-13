using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Common;

namespace GroupChat.GAgent.Feature.Blackboard.LogEvent;

[GenerateSerializer]
public class BlackboardState:StateBase
{
    [Id(0)] public List<ChatMessage> MessageList = new List<ChatMessage>();

    public void Apply(AddChatHistoryLogEvent @event)
    {
        var message = new ChatMessage()
        {
            AgentName = @event.AgentName, Content = @event.Content, MemberId = @event.MemberId,
            MessageType = @event.MessageType
        };
        
        MessageList.Add(message);
    }
}