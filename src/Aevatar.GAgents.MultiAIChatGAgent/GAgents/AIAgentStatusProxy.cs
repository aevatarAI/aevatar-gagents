using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.GAgents.ProxySEvents;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MultiAIChatGAgent.GAgents;

[GAgent]
public class AIAgentStatusProxy :
        AIGAgentBase<AIAgentStatusProxyState, AIAgentStatusProxyLogEvent, EventBase, AIAgentStatusProxyConfig>,
        IAIAgentStatusProxy
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AIGAgent supporting state management");
    }

    protected sealed override async Task PerformConfigAsync(AIAgentStatusProxyConfig configuration)
    {
        await InitializeAsync(
            new InitializeDto()
            {
                Instructions = configuration.Instructions,
                LLMConfig = configuration.LLMConfig,
                StreamingModeEnabled = configuration.StreamingModeEnabled,
                StreamingConfig = configuration.StreamingConfig
            });
        if (configuration.RequestRecoveryDelay != null)
        {
            RaiseEvent(new SetRecoveryDelayLogEvent
            {
                RecoveryDelay = default
            });
            await ConfirmEvents();
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (State.IsAvailable)
        {
            return true;
        }

        if (State.UnavailableSince == null)
        {
            Logger.LogDebug($"[AIAgentStatusProxy][IsAvailableAsync] State.UnavailableSince is null");
            return true;
        }
        
        var now = DateTime.UtcNow;
        var unavailableSince = State.UnavailableSince;
        var timeElapsed = now - unavailableSince;
        if (timeElapsed > State.RecoveryDelay)
        {
            RaiseEvent(new SetAvailableLogEvent());
            await ConfirmEvents();
            return true;
        }

        return false;
    }

    public Task Callback()
    {
        
    }

    public Task<TimeSpan?> GetUnavailableDurationAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<T> ExecuteAsync<T>(Func<AIGAgentBase<AIAgentStatusProxyState, AIAgentStatusProxyLogEvent, EventBase, AIAgentStatusProxyConfig>, Task<T>> func)
    {
        try
        {
            var result = await func(this);
            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task ExecuteAsync(Func<AIGAgentBase<AIAgentStatusProxyState, AIAgentStatusProxyLogEvent, EventBase, AIAgentStatusProxyConfig>, Task> func)
    {
        try
        {
            await func(this);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    
    protected override void AIGAgentTransitionState(AIAgentStatusProxyState state,
        StateLogEventBase<AIAgentStatusProxyLogEvent> @event)
    {
        switch (@event)
        {
            case SetRecoveryDelayLogEvent setRecoveryDelayLogEvent:
                State.RecoveryDelay = setRecoveryDelayLogEvent.RecoveryDelay;
                break;
            case SetAvailableLogEvent setAvailableLogEvent:
                State.IsAvailable = true;
                State.UnavailableSince = null;
                break;
        }
    }
}

public interface IAIAgentStatusProxy : IGAgent, IAIGAgent
{
    Task<bool> IsAvailableAsync();
    Task<TimeSpan?> GetUnavailableDurationAsync();
    Task<T> ExecuteAsync<T>(
        Func<AIGAgentBase<AIAgentStatusProxyState, AIAgentStatusProxyLogEvent, EventBase, AIAgentStatusProxyConfig>,
            Task<T>> method);
    Task ExecuteAsync(
        Func<AIGAgentBase<AIAgentStatusProxyState, AIAgentStatusProxyLogEvent, EventBase, AIAgentStatusProxyConfig>,
            Task> method);
}