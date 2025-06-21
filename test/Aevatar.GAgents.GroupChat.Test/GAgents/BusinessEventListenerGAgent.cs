using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.GAgent.Dto;
using Microsoft.Extensions.Logging;
using Aevatar.Core;
using Orleans.Streams;

namespace Aevatar.GAgents.GroupChat.Test.GAgents;

[GenerateSerializer]
public class BusinessEventListenerConfigDto : ConfigurationBase
{
    [Id(0)] public string Name { get; set; } = string.Empty;
}

[GenerateSerializer]
public class BusinessEventListenerState : StateBase
{
    [Id(0)] public List<EventBase> ReceivedEvents { get; set; } = new List<EventBase>();
    [Id(1)] public string Name { get; set; } = string.Empty;
}

[GenerateSerializer]
public class BusinessEventListenerLogEvent : StateLogEventBase<BusinessEventListenerLogEvent>
{
}

[GenerateSerializer]
public class EventReceivedLogEvent : BusinessEventListenerLogEvent
{
    [Id(0)] public EventBase ReceivedEvent { get; set; }
    [Id(1)] public DateTime ReceivedTime { get; set; } = DateTime.UtcNow;
}

[GenerateSerializer]
public class SetNameLogEvent : BusinessEventListenerLogEvent
{
    [Id(0)] public string Name { get; set; } = string.Empty;
}

[GAgent]
public class BusinessEventListenerGAgent : GAgentBase<BusinessEventListenerState, BusinessEventListenerLogEvent, EventBase, BusinessEventListenerConfigDto>, IBusinessEventListenerGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Business Event Listener GAgent for Testing");
    }

    [EventHandler]
    public async Task HandleEventAsync(WorkflowCompletionBusinessPushEvent @event)
    {
        Logger.LogInformation("[BusinessEventListenerGAgent] Received WorkflowCompletionBusinessPushEvent: WorkflowId={WorkflowId}, BlackboardId={BlackboardId}", 
            @event.WorkflowId, @event.BlackboardId);

        RaiseEvent(new EventReceivedLogEvent() 
        { 
            ReceivedEvent = @event,
            ReceivedTime = DateTime.UtcNow 
        });
        await ConfirmEvents();
    }

    protected override async Task PerformConfigAsync(BusinessEventListenerConfigDto configuration)
    {
        Logger.LogInformation("[BusinessEventListenerGAgent] Configuring with name: {Name}", configuration.Name);
        
        RaiseEvent(new SetNameLogEvent() { Name = configuration.Name });
        await ConfirmEvents();
        
        Logger.LogInformation("[BusinessEventListenerGAgent] Configuration completed");
    }

    protected override void GAgentTransitionState(BusinessEventListenerState state, StateLogEventBase<BusinessEventListenerLogEvent> @event)
    {
        switch (@event)
        {
            case EventReceivedLogEvent eventReceivedLogEvent:
                state.ReceivedEvents.Add(eventReceivedLogEvent.ReceivedEvent);
                Logger.LogDebug("[BusinessEventListenerGAgent] Event recorded. Total events: {Count}", state.ReceivedEvents.Count);
                break;
            case SetNameLogEvent setNameLogEvent:
                state.Name = setNameLogEvent.Name;
                Logger.LogDebug("[BusinessEventListenerGAgent] Name set to: {Name}", state.Name);
                break;
        }
    }
}

public interface IBusinessEventListenerGAgent : IStateGAgent<BusinessEventListenerState>
{
} 