using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.AIHttpAsyncWoker;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Agent;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent, IGrainAsyncHandler<AIHttpAsyncResponse>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    protected async Task<bool> PromptHttpAsync(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null, bool ifAsync = true)
    {
        var request = new AIHttpAsyncRequest()
        {
            LlmConfig = State.LLM,
            Instructions = State.PromptTemplate,
            VectorId = this.GetGrainId().ToString().Replace("/", ""),
            StreamingConfig = State.StreamingConfig,
            Content = prompt,
            History = history,
            IfUseKnowledge = State.IfUpsertKnowledge,
            PromptSettings = promptSettings,
            Context = context,
        };

        return await CreateLongRunTaskAsync<AIHttpAsyncRequest, AIHttpAsyncResponse>(request, ifAsync);
    }

    public async Task HandleStreamAsync(AIHttpAsyncResponse arg)
    {
        if (arg.TokenUsageStatistics != null)
        {
            var tokenUsage = new TokenUsageStateLogEvent()
            {
                GrainId = this.GetPrimaryKey(),
                InputToken = arg.TokenUsageStatistics.InputToken,
                OutputToken = arg.TokenUsageStatistics.OutputToken,
                TotalUsageToken = arg.TokenUsageStatistics.TotalUsageToken,
                CreateTime = arg.TokenUsageStatistics.CreateTime
            };

            RaiseEvent(tokenUsage);
        }

        await AIChatHttpResponseHandleAsync(arg.Context, arg.ErrorEnum, arg.ErrorMessage, arg.ResponseContent);
    }

    protected virtual Task AIChatHttpResponseHandleAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        string? content)
    {
        return Task.CompletedTask;
    }
}