using Aevatar.Core;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Blackboard;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using GroupChat.GAgent.SEvent;
using Microsoft.Extensions.Logging;

namespace GroupChat.GAgent;

public abstract class GroupMemberGAgent : GAgentBase<GroupMemberState, GroupMemberLogEvent>, IGroupMember
{
    protected GroupMemberGAgent(ILogger<GroupMemberGAgent> logger) : base(logger)
    {
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestEvent @event)
    {
        var history = await GetMessageFromBlackboard(@event.BlackboardId);
        var score = await EvaluationInterestValueAsync(@event.BlackboardId, history);

        await PublishAsync(new EvaluationInterestResponseEvent()
        {
            MemberId = this.GetPrimaryKey(), BlackboardId = @event.BlackboardId, InterestValue = score,
            ChatTerm = @event.ChatTerm
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(SpeechEvent @event)
    {
        if (@event.Speaker != this.GetPrimaryKey())
        {
            return;
        }

        var history = await GetMessageFromBlackboard(@event.BlackboardId);
        var talkResponse = await SpeechAsync(@event.BlackboardId, history);
        await PublishAsync(new SpeechResponseEvent()
        {
            BlackboardId = @event.BlackboardId, MemberId = this.GetPrimaryKey(), MemberName = State.MemberName,
            TalkResponse = talkResponse
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(GroupChatFinishEvent @event)
    {
        await GroupChatFinishAsync(@event.BlackboardId);
    }

    [EventHandler]
    public async Task HandleEventAsync(CoordinatorPingEvent @event)
    {
        if (await IgnoreBlackboardPingEvent(@event.BlackboardId) == false)
        {
            await PublishAsync(new CoordinatorPongEvent() { BlackboardId = @event.BlackboardId, MemberId = this.GetPrimaryKey(), MemberName = State.MemberName});
        }
    }

    protected abstract Task<int> EvaluationInterestValueAsync(Guid blackboardId, List<ChatMessage> messages);

    protected abstract Task<TalkResponse> SpeechAsync(Guid blackboardId, List<ChatMessage> messages);

    protected virtual Task GroupChatFinishAsync(Guid blackboardId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task<bool> IgnoreBlackboardPingEvent(Guid blackboardId)
    {
        return Task.FromResult(false);
    }
    
    public async Task SetMemberName(string agentName)
    {
        RaiseEvent(new SetMemberNameLogEvent() { MemberName = agentName });

        await ConfirmEvents();
    }

    protected async Task<List<ChatMessage>> GetMessageFromBlackboard(Guid blackboardId)
    {
        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(blackboardId);
        var history = await blackboard.GetContent();

        return history;
    }
}

public interface IGroupMember : IGAgent
{
    Task SetMemberName(string agentName);
}