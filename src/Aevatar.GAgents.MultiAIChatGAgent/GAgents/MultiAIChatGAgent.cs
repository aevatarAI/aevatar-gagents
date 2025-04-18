using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;
using Aevatar.GAgents.MultiAIChatGAgent.GAgents.ProxySEvents;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.GAgents.MultiAIChatGAgent.GAgents;

public abstract class MultiAIChatGAgent<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IMultiAIChatGAgent
    where TState : MultiAIChatGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : MultiAIChatConfig
{
    protected List<IAIAgentStatusProxy> AIAgentStatusProxies = new();

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat Agent");
    }

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        if (configuration.LLMConfigs.IsNullOrEmpty())
        {
            Logger.LogDebug($"[MultiAIChatGAgent][PerformConfigAsync] LLMConfigs is null or empty.");
            return;
        }

        var aiAgentIds = new List<Guid>();
        Logger.LogDebug($"[MultiAIChatGAgent][PerformConfigAsync] LLMConfigs.count={configuration.LLMConfigs.Count}");
        foreach (var llmConfigDto in configuration.LLMConfigs)
        {
            var aiAgentStatusProxy =
                GrainFactory
                    .GetGrain<IAIAgentStatusProxy>(Guid.NewGuid());
            await aiAgentStatusProxy.ConfigAsync(new AIAgentStatusProxyConfig
            {
                Instructions = configuration.Instructions,
                LLMConfig = llmConfigDto,
                StreamingModeEnabled = configuration.StreamingModeEnabled,
                StreamingConfig = configuration.StreamingConfig,
                RequestRecoveryDelay = configuration.RequestRecoveryDelay
            });

            Logger.LogDebug(
                $"[MultiAIChatGAgent][PerformConfigAsync] MultiAIChatgAgentId: {this.GetPrimaryKey().ToString()}, AIAgentStatusProxyId: {aiAgentStatusProxy.GetPrimaryKey().ToString()}");

            AIAgentStatusProxies.Add(aiAgentStatusProxy);
            aiAgentIds.Add(aiAgentStatusProxy.GetPrimaryKey());
        }

        var maxHistoryCount = configuration.MaxHistoryCount;
        if (maxHistoryCount > 100)
        {
            maxHistoryCount = 100;
        }

        if (maxHistoryCount <= 0)
        {
            maxHistoryCount = 10;
        }

        RaiseEvent(new SetMultiAIChatConfigLogEvent
        {
            MaxHistoryCount = maxHistoryCount,
            AIAgentIds = aiAgentIds
        });
        await ConfirmEvents();
    }

    public async Task<List<ChatMessage>?> ChatAsync(string message, ExecutionPromptSettings? promptSettings = null,
        AIChatContextDto? aiChatContextDto = null)
    {
        var aiAgentStatusProxy = await GetAIAgentStatusProxy();
        if (aiAgentStatusProxy == null)
        {
            Logger.LogError($"There is no available AI Agent. {this.GetPrimaryKey().ToString()}");
            throw new SystemException("There is no available AI Agent.");
        }

        Func<AIGAgentBase<AIAgentStatusProxyState, AIAgentStatusProxyLogEvent, EventBase, AIAgentStatusProxyConfig>,
            Task<List<ChatMessage>?>> func = async (aiAgent) =>
        {
            var result =
                await aiAgent.ChatWithHistory(message, State.ChatHistory, promptSettings, context: aiChatContextDto);
            return result;
        };

        var result = await aiAgentStatusProxy.ExecuteAsync(func);
        
        if (result is not { Count: > 0 }) return result;

        var chatMessages = new List<ChatMessage>();
        chatMessages.Add(new ChatMessage() { ChatRole = ChatRole.User, Content = message });
        chatMessages.AddRange(result);

        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = chatMessages });

        await ConfirmEvents();

        return result;
    }

    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        if (!State.AIAgentIds.IsNullOrEmpty())
        {
            Logger.LogDebug(
                $"[MultiAIChatGAgent][OnGAgentActivateAsync] init AIAgentStatusProxies..{JsonConvert.SerializeObject(State.AIAgentIds)}");
            AIAgentStatusProxies =
                new List<IAIAgentStatusProxy>();
            foreach (var agentId in State.AIAgentIds)
            {
                AIAgentStatusProxies.Add(GrainFactory
                    .GetGrain<IAIAgentStatusProxy>(agentId));
            }
        }

        return Task.CompletedTask;
    }

    private async Task<IAIAgentStatusProxy?> GetAIAgentStatusProxy()
    {
        if (AIAgentStatusProxies.IsNullOrEmpty())
        {
            return null;
        }

        foreach (var aiAgentStatusProxy in AIAgentStatusProxies)
        {
            if (!await aiAgentStatusProxy.IsAvailableAsync())
            {
                continue;
            }

            return aiAgentStatusProxy;
        }
        return null;
    }


    [GenerateSerializer]
    public class AddChatHistoryLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<ChatMessage> ChatList { get; set; }
    }

    [GenerateSerializer]
    public class SetMultiAIChatConfigLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public int MaxHistoryCount { get; set; }
        [Id(1)] public List<Guid> AIAgentIds { get; set; }
    }

    protected override void GAgentTransitionState(TState state,
        StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddChatHistoryLogEvent setChatHistoryLog:
                if (setChatHistoryLog.ChatList.Count > 0)
                {
                    state.ChatHistory.AddRange(setChatHistoryLog.ChatList);
                }

                if (state.ChatHistory.Count() > state.MaxHistoryCount)
                {
                    state.ChatHistory.RemoveRange(0, state.ChatHistory.Count() - state.MaxHistoryCount);
                }

                break;
            case SetMultiAIChatConfigLogEvent setMultiAiChatConfigLogEvent:
                state.MaxHistoryCount = setMultiAiChatConfigLogEvent.MaxHistoryCount;
                break;
        }
    }
}

public interface IMultiAIChatGAgent : IGAgent
{
    Task<List<ChatMessage>?> ChatAsync(string message,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? aiChatContextDto = null);
}