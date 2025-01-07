using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using AevatarGAgents.PumpFun.Agent.GEvents;
using AevatarGAgents.PumpFun.EventDtos;
using AevatarGAgents.PumpFun.Grains;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;

namespace AevatarGAgents.PumpFun.Agent;

[Description("Handle PumpFun")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class PumpFunGAgent : GAgentBase<PumpFunGAgentState, GEventBase>, IPumpFunGAgent
{
    private readonly ILogger<PumpFunGAgent> _logger;
    public PumpFunGAgent(ILogger<PumpFunGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Represents an agent responsible for informing other agents when a PumpFun thread is published.");
    }

    [EventHandler]
    public async Task HandleEventAsync(PumpFunSendMessageEvent @event)
    {
        _logger.LogInformation("PumpFunSendMessageEvent:" + JsonConvert.SerializeObject(@event));
        if (@event.ReplyId != null)
        {
            PumpFunSendMessageGEvent pumpFunSendMessageGEvent = new PumpFunSendMessageGEvent()
            {
                Id = Guid.Parse(@event.ReplyId),
                ReplyId = @event.ReplyId,
                ReplyMessage = @event.ReplyMessage
            };
            
            RaiseEvent(pumpFunSendMessageGEvent);
            await ConfirmEvents();
            _logger.LogInformation("PumpFunSendMessageEvent2:" + JsonConvert.SerializeObject(@pumpFunSendMessageGEvent));
            await GrainFactory.GetGrain<IPumpFunGrain>(Guid.Parse(@event.ReplyId))
                .SendMessageAsync(@event.ReplyId, @event.ReplyMessage);
            _logger.LogInformation("PumpFunSendMessageEvent3,grainId:" + 
                                   GrainFactory.GetGrain<IPumpFunGrain>(Guid.Parse(@event.ReplyId)).GetGrainId());

        }
    }
    
    public async Task SetPumpFunConfig(string chatId)
    {
        _logger.LogInformation("PumpFunGAgent SetPumpFunConfig, chatId:" + chatId);
        RaiseEvent(new SetPumpFunConfigEvent()
        {
            ChatId = chatId
        });
        await ConfirmEvents();
        _logger.LogInformation("PumpFunGAgent SetPumpFunConfig2, chatId:" + chatId);
    }

}

public interface IPumpFunGAgent : IStateGAgent<PumpFunGAgentState>
{ 
    Task SetPumpFunConfig(string chatId);
    
}