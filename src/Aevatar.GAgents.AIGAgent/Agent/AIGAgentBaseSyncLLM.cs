using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Feature.SyncLLMWorker;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Agent;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    protected async Task SyncChatWithHistoryAsync(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null)
    {
        var llmConfig = GetLLMConfigFromState();
        if (llmConfig == null)
        {
            Logger.LogError("[AIGAgentBase][ChatWithHistorySync] llmconfig == null");
            return;
        }

        var llmRequest = new SyncLLMRequestEvent()
        {
            GrainId = GetLLMUniqueKey(), Instructions = State.PromptTemplate, IfUseKnowledge = State.IfUpsertKnowledge,
            LLMConfig = llmConfig, Prompt = prompt, History = history, PromptSettings = promptSettings,
            ContextDto = context
        };
        await CreateLongRunTaskAsync<SyncLLMRequestEvent, SyncLLMResponseEvent>(llmRequest);

        RaiseEvent(new LongTaskRequestLogEvent() { RequestId = llmRequest.RequestId });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(SyncLLMResponseEvent eventData)
    {
        if (State.CurrentLLMReqeustIds.Contains(eventData.RequestId) == false)
        {
            return;
        }
        
        var eventList = new List<StateLogEventBase<TStateLogEvent>>();
        if (eventData.TokenUsageStatistics != null)
        {
            var tokenUsage = new TokenUsageStateLogEvent()
            {
                GrainId = this.GetPrimaryKey(),
                InputToken = eventData.TokenUsageStatistics.InputToken,
                OutputToken = eventData.TokenUsageStatistics.OutputToken,
                TotalUsageToken = eventData.TokenUsageStatistics.TotalUsageToken,
                CreateTime = eventData.TokenUsageStatistics.CreateTime
            };
            eventList.Add(tokenUsage);
        }
        
        eventList.Add(new LongTaskResponseLogEvent(){RequestId = eventData.RequestId});
        
        RaiseEvents(eventList);
        await ConfirmEvents();

        await SyncLLMResponseHandlerAsync(eventData.ChatResponseList, eventData.ErrorMessage, eventData.ContextDto);
    }

    protected virtual Task SyncLLMResponseHandlerAsync(List<ChatMessage>? chatResponseList, string errorMessage,
        AIChatContextDto? context = null)
    {
        return Task.CompletedTask;
    }

    public Task<IBrain?> GetBrainAsync()
    {
        return Task.FromResult(_brain);
    }

    [GenerateSerializer]
    public class LongTaskRequestLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Guid RequestId { get; set; }
    }

    [GenerateSerializer]
    public class LongTaskResponseLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Guid RequestId { get; set; }
    }
}