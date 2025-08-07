using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    [EventHandler]
    public async Task HandleSendConfigEventAsync(AgentConfigEvent @event)
    {
        await TraceEventHandlerAsync(@event, async () =>
        {
            if(_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate config event detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            LogEventInfo("SendConfigEvent received: UniqueId={UniqueId}, ModelId={ModelId}", 
                @event.UniqueId, @event.Configuration.Model.ModelId);
            
            if (!_receivedMessageIds.Add(@event.UniqueId))
            {
                LogEventDebug("Duplicate config event detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }
            
            RaiseEventWithTracing(new UpdateSendConfigEvent()
            {
                Event = @event
            });
            await ConfirmEventsWithTracing();
        });
    }

    [EventHandler]
    public async Task HandleUserMessageEventAsync(UserMessageEvent @event)
    {
        // Check if the message is for this agent
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // LogEventDebug("Message not for this agent, ignoring");
            return;
        }
        
        await TraceEventHandlerAsync(@event, async () =>
        {
            
            LogEventDebug("UserMessageEvent received: UniqueId={UniqueId}, TargetAgentId={TargetAgentId}, CallId={CallId}, Content={Content}",
                @event.UniqueId, @event.TargetAgentId, @event.CallId, @event.Content?.Substring(0, Math.Min(@event.Content.Length, 100)));
            
            if(_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate user message event detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            if (!_receivedMessageIds.Add(@event.UniqueId))
            {
                LogEventDebug("Duplicate message detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            RaiseEventWithTracing(new ReceiveUserMessageEvent
            {
                Event = @event
            });
            await ConfirmEventsWithTracing();
        });
    }

    [EventHandler]
    public async Task HandleAgentMessageEventAsync(AgentMessageEvent @event)
    {
        // Check if the message is for this agent
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // LogEventDebug("Message not for this agent, ignoring");
            return;
        }
        
        await TraceEventHandlerAsync(@event, async () =>
        {
            LogEventDebug("AgentMessageEvent received: UniqueId={UniqueId}, TargetAgentId={TargetAgentId}, CallId={CallId}, Content={Content} with {ArtifactCount} artifacts",
                @event.UniqueId, @event.TargetAgentId, @event.CallId, @event.Content?.Substring(0, Math.Min(@event.Content.Length, 100)), @event.Artifacts.Count);
            if (_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate agent message detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            _receivedMessageIds.Add(@event.UniqueId);

            RaiseEventWithTracing(new ReceiveAgentMessageEvent()
            {
                Event = @event
            });
            await ConfirmEventsWithTracing();
        });
    }

    [EventHandler]
    public async Task HandleSelfReportEventAsync(SelfReportEvent @event)
    {
        // Check if the message is for this agent
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // LogEventDebug("Self report not for this agent, ignoring");
            return;
        }
        
        await TraceEventHandlerAsync(@event, async () =>
        {
            LogEventDebug("SelfReportEvent received: UniqueId={UniqueId}, TargetAgentId={TargetAgentId}, ReportingAgent={ReportingAgent}, AgentType={AgentType}",
                @event.UniqueId, @event.TargetAgentId, @event.SelfReport.AgentId, @event.SelfReport.AgentType);

            if (_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate self report detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            _receivedMessageIds.Add(@event.UniqueId);

            RaiseEventWithTracing(new UpdateChildEvent()
            {
                LastChildDescriptor = @event.SelfReport
            });
            await ConfirmEventsWithTracing();
        });
    }
}