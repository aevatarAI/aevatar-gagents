using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.GEvents;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Orleans;
using Orleans.SyncWork;

namespace Aevatar.GAgents.AIGAgent.Agent;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent, IStreamHandler<AIStreamingResponseGEvent>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    protected async Task ChatWithStreamAsync(string prompt,
        Func<AIStreamingResponseGEvent, Task> streamHandler, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, CancellationToken cancellationToken = default,
        AIChatContextDto? context = null)
    {
        var request = new StreamRequest();
        await CreateStreamLongRunTaskAsync<StreamRequest, AIStreamingResponseGEvent>(request);
    }

    protected async Task CreateStreamLongRunTaskAsync<TRequest, TResponse>(TRequest request)
    {
        try
        {
            var syncWorker = GrainFactory.GetGrain<IStreamAsyncWorker<TRequest, TResponse>>(Guid.NewGuid());
            await syncWorker.SetLongRunTaskAsync(this.GetGrainId());
            await syncWorker.StartWorkAndPollUntilResult(request);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating long run task: {ex.Message}");
            throw;
        }
    }

    public async Task HandleStreamAsync(AIStreamingResponseGEvent arg)
    {
        if (arg.IfAggregationMsg)
        {
            // todo:
        }

        await AIHandlerStreamAsync(arg);
    }

    public virtual Task AIHandlerStreamAsync(AIStreamingResponseGEvent arg)
    {
        return Task.CompletedTask;
    }
}