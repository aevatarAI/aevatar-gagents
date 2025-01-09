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
public class AElfGAgent : GAgentBase<AElfAgentGState, TransactionSEvent>, IAElfAgent
{
    public AElfGAgent(ILogger<AElfGAgent> logger) : base(logger)
    {
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("An agent to inform other agents when a aelf thread is published.");
    }

    [EventHandler]
    protected async Task ExecuteAsync(CreateTransactionEvent gEventData)
    {
       var gEvent = new CreateTransactionSEvent
        {
            ChainId = gEventData.ChainId,
            SenderName = gEventData.SenderName,
            ContractAddress = gEventData.ContractAddress,
            MethodName = gEventData.MethodName,
        };
        RaiseEvent(gEvent);
        await ConfirmEvents();
        _= GrainFactory.GetGrain<ITransactionGrain>(gEvent.Id).SendAElfTransactionAsync(
            new SendTransactionDto
            {
                Id = gEvent.Id,
                ChainId = gEventData.ChainId,
                SenderName = gEventData.SenderName,
                ContractAddress = gEventData.ContractAddress,
                MethodName = gEventData.MethodName,
                Param = gEventData.Param
            });
        Logger.LogInformation("ExecuteAsync: AElf {MethodName}", gEventData.MethodName);
    }
    
    [EventHandler]
    public Task ExecuteAsync(SendTransactionCallBackEvent gEventData)
    {
        RaiseEvent(new SendTransactionSEvent
        {
            CreateTransactionGEventId = gEventData.CreateTransactionGEventId,
            ChainId = gEventData.ChainId,
            TransactionId = gEventData.TransactionId
        });
       
        _= GrainFactory.GetGrain<ITransactionGrain>(gEventData.Id).LoadAElfTransactionResultAsync(
            new QueryTransactionDto
            {
                CreateTransactionGEventId = gEventData.CreateTransactionGEventId,
                ChainId = gEventData.ChainId,
                TransactionId = gEventData.TransactionId
            });
        return Task.CompletedTask;
    }

    [EventHandler]
    public async Task ExecuteAsync(QueryTransactionCallBackEvent gEventData)
    {
        if (gEventData.IsSuccess)
        {
            RaiseEvent(new TransactionSuccessSEvent
            {
                CreateTransactionGEventId = gEventData.CreateTransactionGEventId
            });
        }
        else
        {
            RaiseEvent(new TransactionFailedSEvent()
            {
                CreateTransactionGEventId = gEventData.CreateTransactionGEventId,
                Error = gEventData.Error
            });
        }
        await ConfirmEvents();
    }

    public async Task ExecuteTransactionAsync(CreateTransactionEvent gEventData)
    {
        await ExecuteAsync( gEventData);
    }

    public async Task<AElfAgentGState> GetAElfAgentDto()
    {
        AElfAgentDto aelfAgentDto = new AElfAgentDto();
        aelfAgentDto.Id = State.Id;
        aelfAgentDto.PendingTransactions = State.PendingTransactions;
        return aelfAgentDto;
    }
    

    
    protected Task ExecuteAsync(TransactionSEvent eventData)
    {
        return Task.CompletedTask;
    }
}

public interface IAElfAgent : IGrainWithGuidKey
{ 
    Task ExecuteTransactionAsync(CreateTransactionEvent gEventData);
    Task<AElfAgentGState> GetAElfAgentDto();
}

