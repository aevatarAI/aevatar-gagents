using Aevatar.Core;
using GroupChat.GAgent.Feature.Blackboard.LogEvent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Blackboard.Dto;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using GroupChat.GAgent.GEvent;

namespace GroupChat.GAgent.Feature.Blackboard;

public class BlackboardAgent : GAgentBase<BlackboardState, BlackboardLogEvent>,
    IBlackboardGAgent
{
    public BlackboardAgent(ILogger<BlackboardAgent> logger) : base(logger)
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
    public async Task HandleEventAsync(SpeechResponseEvent @event)
    {
        if (@event.BlackboardId != this.GetPrimaryKey())
        {
            return;
        }

        if (@event.TalkResponse.SkipSpeak == false)
        {
            RaiseEvent(new AddChatHistoryLogEvent()
            {
                AgentName = @event.MemberName, MemberId = @event.MemberId, MessageType = MessageType.User,
                Content = @event.TalkResponse.SpeakContent
            });
            await ConfirmEvents();
        }

        var coordinatorAgent = GetICoordinatorGAgent();
        await coordinatorAgent.HandleSpeechResponseEventAsync(@event);
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestResponseEvent @event)
    {
        var coordinatorAgent = GetICoordinatorGAgent();
        await coordinatorAgent.HandleEvaluationInterestResultEventAsync(@event);
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
    public async Task HandleEventAsync(SpeechEventForCoordinator @event)
    {
        // proxy coordinator event to group
        await PublishAsync(new SpeechEvent()
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