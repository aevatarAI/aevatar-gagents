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

        var coordinator = GrainFactory.GetGrain<ICoordinatorGAgent>(this.GetPrimaryKey());
        await RegisterAsync(coordinator);

        await coordinator.StartAsync();
        return true;
    }

    public Task<List<ChatMessage>> GetContent()
    {
        return Task.FromResult(State.MessageList);
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        var coordinatorAgent = GetICoordinatorGAgent();
        if (await coordinatorAgent.CheckChatIsAvailable(@event) == false)
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

        await coordinatorAgent.HandleChatResponseEventAsync(@event);
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestResponseEvent @event)
    {
        var coordinatorAgent = GetICoordinatorGAgent();
        await coordinatorAgent.HandleGetInterestResultEventAsync(@event);
    }

    [EventHandler]
    public async Task HandleEventAsync(CoordinatorPongEvent @event)
    {
        var coordinatorAgent = GetICoordinatorGAgent();
        await coordinatorAgent.HandleCoordinatorPongEventAsync(@event);
    }

    #region Proxy coordinator event to memeber;

    [EventHandler]
    public async Task HandleEventAsync(GroupChatFinishEventForCoordinator @event)
    {
        // proxy coordinator event to group
        await PublishAsync(new GroupChatFinishEvent()
        {
            BlackboardId = @event.BlackboardId
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestEventForCoordinator @event)
    {
        await PublishAsync(new EvaluationInterestEvent()
            { BlackboardId = @event.BlackboardId, ChatTerm = @event.ChatTerm });
    }

    [EventHandler]
    public async Task HandleEventAsync(CoordinatorPingEventForCoordinator @event)
    {
        // proxy coordinator event to group
        await PublishAsync(new CoordinatorPingEvent()
        {
            BlackboardId = @event.BlackboardId,
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatEventForCoordinator @event)
    {
        // proxy coordinator event to group
        await PublishAsync(new ChatEvent()
        {
            BlackboardId = @event.BlackboardId,
            Speaker = @event.Speaker
        });
    }

    private ICoordinatorGAgent GetICoordinatorGAgent()
    {
        return GrainFactory.GetGrain<ICoordinatorGAgent>(this.GetPrimaryKey());
    }

    #endregion
}

public interface IBlackboardGAgent : IGAgent
{
    public Task<bool> SetTopic(string topic);
    public Task<List<ChatMessage>> GetContent();
}