using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.GroupChat;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Microsoft.Extensions.Logging;

namespace GroupChat.GAgent;

public abstract class
    GroupMemberGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IWorkflowUnit
    where TState : GroupMemberState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : GroupMemberConfigDto
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "GroupMemberGAgentBase - Base class for workflow nodes and group chat participants. " +
            "Provides core functionality for responding to coordination events, evaluating interest in participation, " +
            "processing messages from upstream nodes, and contributing to workflow execution. " +
            $"Member Name: {State.MemberName ?? "Not configured"}"
        );
    }

    [EventHandler]
    public async Task HandleEventAsync(EvaluationInterestEvent @event)
    {
        var score = await GetInterestValueAsync(@event.BlackboardId);

        await PublishAsync(new EvaluationInterestResponseEvent()
        {
            MemberId = this.GetPrimaryKey(),
            BlackboardId = @event.BlackboardId,
            InterestValue = score,
            ChatTerm = @event.ChatTerm
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatEvent @event)
    {
        if (@event.Speaker != this.GetPrimaryKey())
        {
            return;
        }

        // var history = await GetCareChatMessagesFromBlackboardAsync(@event.BlackboardId);
        var talkResponse = await ChatAsync(@event.BlackboardId, @event.CoordinatorMessages);
        await PublishAsync(new ChatResponseEvent
        {
            BlackboardId = @event.BlackboardId,
            MemberId = this.GetPrimaryKey(),
            MemberName = State.MemberName,
            ChatResponse = talkResponse,
            Term = @event.Term
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
            await PublishAsync(new CoordinatorPongEvent()
            {
                BlackboardId = @event.BlackboardId,
                MemberId = this.GetPrimaryKey(),
                MemberName = State.MemberName
            });
        }
    }

    protected abstract Task<int> GetInterestValueAsync(Guid blackboardId);

    protected abstract Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages);

    protected virtual Task GroupChatFinishAsync(Guid blackboardId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task<bool> IgnoreBlackboardPingEvent(Guid blackboardId)
    {
        return Task.FromResult(false);
    }

    [GenerateSerializer]
    public class SetMemberNameLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public string MemberName { get; set; }
    }

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        await base.PerformConfigAsync(configuration);
        RaiseEvent(new SetMemberNameLogEvent { MemberName = configuration.MemberName });
        await ConfirmEvents();
    }

    protected override void AIGAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetMemberNameLogEvent @setMemberNameLogEvent:
                State.MemberName = @setMemberNameLogEvent.MemberName;
                return;
        }

        GroupMemberTransitionState(state, @event);
    }

    protected virtual void GroupMemberTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
    }

    protected async Task<List<ChatMessage>> GetMessageFromBlackboardAsync(Guid blackboardId)
    {
        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(blackboardId);
        var history = await blackboard.GetContent();

        return history;
    }

    #region IWorkflowUnit Implementation

    public virtual async Task EstablishRelationshipAsync(GrainId relatedUnit, string relationship)
    {
        // Base class only records relationships, specific behavior is determined by subclasses
        Logger.LogInformation("Establishing {Relationship} relationship with {RelatedUnit}", relationship, relatedUnit);

        // Subclasses can override this method to handle specific relationship types
        await OnRelationshipEstablishedAsync(relatedUnit, relationship);
    }

    public virtual Task<WorkflowUnitCapabilities> GetCapabilitiesAsync()
    {
        // Return default basic capabilities, subclasses can extend
        return Task.FromResult(new WorkflowUnitCapabilities
        {
            UnitType = GetType().Name,
            ProvidedCapabilities = ["MessageProcessing", "StateManagement"],
            RequiredCapabilities = [],
            Metadata = new Dictionary<string, object>
            {
                ["MemberName"] = State.MemberName.IsNullOrEmpty() ? "Unknown" : State.MemberName,
                ["MemberId"] = this.GetPrimaryKey().ToString()
            }
        });
    }

    public virtual async Task PrepareForExecutionAsync(WorkflowExecutionContext context)
    {
        // Let subclasses decide how to prepare for execution
        Logger.LogInformation("Preparing for workflow execution: {ContextWorkflowId}", context.WorkflowId);

        // Store context for later use
        State.WorkflowContext = context;

        // Subclasses can perform specific preparation work here
        await OnPrepareForExecutionAsync(context);
    }

    /// <summary>
    /// Called when a relationship is established, subclasses can override to handle specific relationships
    /// </summary>
    protected virtual Task OnRelationshipEstablishedAsync(GrainId relatedUnit, string relationship)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when preparing for execution, subclasses can override to perform specific preparation
    /// </summary>
    protected virtual async Task OnPrepareForExecutionAsync(WorkflowExecutionContext context)
    {
        // If this is a workflow-aware AIGAgent, call the appropriate preparation logic
        // ReSharper disable once PatternNeverMatches
        if (this is WorkflowAwareAIGAgentBase<TState, TStateLogEvent, TEvent> workflowAware)
        {
            // Call protected method via reflection (or provide public interface in WorkflowAwareAIGAgentBase)
            var method = typeof(WorkflowAwareAIGAgentBase<TState, TStateLogEvent, TEvent>)
                .GetMethod("OnWorkflowContextReadyAsync",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                await (Task)method.Invoke(workflowAware, [context])!;
            }
        }

        // Subclasses can add their own preparation logic
        await Task.CompletedTask;
    }

    #endregion
}