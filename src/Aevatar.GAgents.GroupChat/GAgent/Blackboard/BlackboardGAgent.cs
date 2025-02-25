using Aevatar.Core;
using GroupChat.GAgent.Feature.Blackboard.LogEvent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Coordinator.GEvent;

namespace GroupChat.GAgent.Feature.Blackboard;

public class BlackboardGAgent : GAgentBase<BlackboardState, BlackboardLogEvent>,
    IBlackboardGAgent
{
    public BlackboardGAgent(ILogger<BlackboardGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("blackboard");
    }

    public async Task<bool> SetTopic(string topic)
    {
        if (State.MessageList.Count > 0)
        {
            return false;
        }
        
        RaiseEvent(new AddChatHistoryLogEvent()
            { MessageType = MessageType.BlackboardTopic, Content = topic });
        await ConfirmEvents();
        return true;
    }

    public Task<List<ChatMessage>> GetContent()
    {
        return Task.FromResult(State.MessageList);
    }

    [EventHandler]
    public async Task HandleEventAsync(CoordinatorConfirmChatResponse @event)
    {
        if (@event.BlackboardId != this.GetPrimaryKey())
        {
            return;
        }

        if (@event.ChatResponse.Skip == false)
        {
            RaiseEvent(new AddChatHistoryLogEvent()
            {
                AgentName = @event.MemberName, MemberId = @event.MemberId, MessageType = MessageType.User,
                Content = @event.ChatResponse.Content
            });
            await ConfirmEvents();
        }
    }
}

public interface IBlackboardGAgent : IGAgent
{
    public Task<bool> SetTopic(string topic);
    public Task<List<ChatMessage>> GetContent();
}