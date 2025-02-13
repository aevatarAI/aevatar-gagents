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

public class BlackboardAgent<T> : GAgentBase<BlackboardState, BlackboardLogEvent, EventBase, BlackboardInitDto>, IBlackboardGAgent where T:CoordinatorGAgentBase
{
    public BlackboardAgent(ILogger logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("blackboard");
    }

    public Task<List<ChatMessage>> GetContent()
    {
        return Task.FromResult(State.MessageList);
    }

    [EventHandler]
    public async Task HandleEventAsync(SpeechResponseEvent @event)
    {
        RaiseEvent(new AddChatHistoryLogEvent()
        {
            AgentName = @event.MemberName, MemberId = @event.MemberId, MessageType = MessageType.User, Content = @event.TalkResponse.TalkContent
        });
        await ConfirmEvents();

        var coordinatorAgent = GetICoordinatorGAgent();
        await coordinatorAgent.HandleSpeechResponseEventAsync(@event);
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestResultEvent @event)
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
    
    [EventHandler]
    public async Task HandleEventAsync(EventFromCoordinatorBase @event)
    {
        // proxy coordinator event to group
        await PublishAsync(@event);
    }

    public override async Task InitializeAsync(BlackboardInitDto initializationEvent)
    {
        RaiseEvent(new AddChatHistoryLogEvent(){MessageType= MessageType.BlackboardTopic, Content = initializationEvent.Topic});
        await ConfirmEvents();
        
        var coordinator = GrainFactory.GetGrain<ICoordinatorGAgent>(this.GetPrimaryKey());
        await SubscribeToAsync(coordinator);
    }

    private ICoordinatorGAgent GetICoordinatorGAgent()
    {
        return GrainFactory.GetGrain<ICoordinatorGAgent>(this.GetPrimaryKey());
    }
}

public interface IBlackboardGAgent : IGrainWithGuidKey
{
    public Task<List<ChatMessage>> GetContent();
}