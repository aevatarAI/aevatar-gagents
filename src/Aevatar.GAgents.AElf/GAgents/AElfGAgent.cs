using System;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.GAgents.AElf.Agent.Event;
using Aevatar.GAgents.AElf.Agent.Events;
using Aevatar.GAgents.AElf.Agent.GEvents;
using Aevatar.GAgents.AElf.Agent.Grains;
using Aevatar.GAgents.AElf.Dto;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AElf.Agent;

[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(AElfGAgent))]
public class AElfGAgent : GAgentBase<AElfAgentGState, TransactionStateLogEvent>, IAElfAgent
{
    public AElfGAgent(ILogger<AElfGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("An agent to inform other agents when a aelf thread is published.");
    }

    [EventHandler]
    protected async Task ExecuteAsync(CreateTransactionGEvent gGEventData)
    {
        var createTransactionStateLogEvent = new CreateTransactionStateLogEvent
        {
            ChainId = gGEventData.ChainId,
            SenderName = gGEventData.SenderName,
            ContractAddress = gGEventData.ContractAddress,
            MethodName = gGEventData.MethodName,
        };
        RaiseEvent(createTransactionStateLogEvent);
        await ConfirmEvents();
        _ = GrainFactory.GetGrain<ITransactionGrain>(createTransactionStateLogEvent.Id).SendAElfTransactionAsync(
            new SendTransactionDto
            {
                Id = createTransactionStateLogEvent.Id,
                ChainId = gGEventData.ChainId,
                SenderName = gGEventData.SenderName,
                ContractAddress = gGEventData.ContractAddress,
                MethodName = gGEventData.MethodName,
                Param = gGEventData.Param
            });
        Logger.LogInformation("ExecuteAsync: AElf {MethodName}", gGEventData.MethodName);
    }

    [EventHandler]
    public Task ExecuteAsync(SendTransactionCallBackGEvent gGEventData)
    {
        RaiseEvent(new SendTransactionStateLogEvent
        {
            CreateTransactionGEventId = gGEventData.CreateTransactionGEventId,
            ChainId = gGEventData.ChainId,
            TransactionId = gGEventData.TransactionId
        });

        _ = GrainFactory.GetGrain<ITransactionGrain>(gGEventData.Id).LoadAElfTransactionResultAsync(
            new QueryTransactionDto
            {
                CreateTransactionGEventId = gGEventData.CreateTransactionGEventId,
                ChainId = gGEventData.ChainId,
                TransactionId = gGEventData.TransactionId
            });
        return Task.CompletedTask;
    }

    [EventHandler]
    public async Task ExecuteAsync(QueryTransactionCallBackGEvent gGEventData)
    {
        if (gGEventData.IsSuccess)
        {
            RaiseEvent(new TransactionSuccessStateLogEvent
            {
                CreateTransactionGEventId = gGEventData.CreateTransactionGEventId
            });
        }
        else
        {
            RaiseEvent(new TransactionFailedStateLogEvent()
            {
                CreateTransactionGEventId = gGEventData.CreateTransactionGEventId,
                Error = gGEventData.Error
            });
        }

        await ConfirmEvents();
    }

    public async Task ExecuteTransactionAsync(CreateTransactionGEvent gGEventData)
    {
        await ExecuteAsync(gGEventData);
    }

    public async Task<AElfAgentGState> GetAElfAgentDto()
    {
        AElfAgentDto aelfAgentDto = new AElfAgentDto();
        aelfAgentDto.Id = State.Id;
        aelfAgentDto.PendingTransactions = State.PendingTransactions;
        return aelfAgentDto;
    }


    protected Task ExecuteAsync(TransactionStateLogEvent eventData)
    {
        return Task.CompletedTask;
    }
}

public interface IAElfAgent : IGrainWithGuidKey
{
    Task ExecuteTransactionAsync(CreateTransactionGEvent gGEventData);
    Task<AElfAgentGState> GetAElfAgentDto();
}