using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.GEvents;
using Aevatar.GAgents.AIGAgent.State;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Orleans;
using Orleans.SyncWork;

namespace Aevatar.GAgents.AIGAgent.Agent;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent, IGrainAsyncHandler<AIStreamChatResponseEvent>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    protected async Task<bool> PromptWithStreamAsync(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? context = null)
    {
        var request = new AIStreamChatRequest()
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

        return await CreateStreamLongRunTaskAsync<AIStreamChatRequest, AIStreamChatResponseEvent>(request);
    }

    protected async Task<bool> CreateStreamLongRunTaskAsync<TRequest, TResponse>(TRequest request)
    {
        try
        {
            var syncWorker = GrainFactory.GetGrain<IGrainAsyncWorker<TRequest, TResponse>>(Guid.NewGuid());
            await syncWorker.SetLongRunTaskAsync(this.GetGrainId());
            var result = await syncWorker.Start(request);
            if (result == false)
            {
                Logger.LogError(
                    $"CreateStreamLongRunTaskAsync run task fail, request info:{JsonConvert.SerializeObject(request)}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError($"CreateStreamLongRunTaskAsync creating long run task error: {ex.Message}");
            throw;
        }
    }

    public async Task HandleStreamAsync(AIStreamChatResponseEvent arg)
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

        await AIChatHandleStreamAsync(arg.Context, arg.ErrorEnum, arg.ErrorMessage, arg.ChatContent);
    }

    protected virtual Task AIChatHandleStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum , string? errorMessage,
        AIStreamChatContent? content)
    {
        return Task.CompletedTask;
    }
}