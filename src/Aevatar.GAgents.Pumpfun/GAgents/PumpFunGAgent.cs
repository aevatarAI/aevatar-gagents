using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;
using Aevatar.GAgents.PumpFun.Agent.GEvents;
using Aevatar.GAgents.PumpFun.EventDtos;
using Aevatar.GAgents.PumpFun.Grains;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;

namespace Aevatar.GAgents.PumpFun.Agent;

[Description("Handle PumpFun")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(PumpFunGAgent))]
public class PumpFunGAgent : GAgentBase<PumpFunGAgentState, PumpfunSEventBase>, IPumpFunGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for informing other agents when a PumpFun thread is published.");
    }

    [EventHandler]
    public async Task TaskHandleEventAsync(PumpFunReceiveMessageEvent @event)
    {
        if (@event.ReplyId.IsNullOrEmpty() || @event.RequestMessage.IsNullOrEmpty())
        {
            Logger.LogError(
                $"[PumpFunGAgent] PumpFunReceiveMessageEvent ReplyId is IsNullOrEmpty, replyId:{@event.ReplyId} requestMessage:{@event.RequestMessage}");
            return;
        }

        var requestId = Guid.NewGuid();
        RaiseEvent(new PumpfunRequestSEvent() { RequestId = requestId });
        await ConfirmEvents();

        await PublishAsync(new SocialGEvent()
        {
            RequestId = requestId,
            Content = @event.RequestMessage,
            MessageId = @event.ReplyId,
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(SocialResponseGEvent @event)
    {
        if (@event.ReplyMessageId.IsNullOrEmpty())
        {
            Logger.LogError($"[PumpFunGAgent] SocialResponseGEvent ReplyMessageId is IsNullOrEmpty");
            return;
        }

        if (@event.RequestId != Guid.Empty)
        {
            if (State.SocialRequestList.Contains(@event.RequestId))
            {
                RaiseEvent(new PumpfunSocialResponseSEvent() { ResponseId = @event.RequestId });
                await ConfirmEvents();
            }
            else
            {
                return;
            }
        }

        Logger.LogDebug("[PumpFunGAgent] HandleEventAsync SocialResponseEvent, content: {text}, id: {id}",
            @event.ResponseContent, @event.ReplyMessageId);

        await GrainFactory.GetGrain<IPumpFunGrain>(Guid.Parse(@event.ReplyMessageId))
            .SendMessageAsync(@event.ReplyMessageId, @event.ResponseContent);
    }

    [EventHandler]
    public async Task HandleEventAsync(PumpFunSendMessageEvent @event)
    {
        Logger.LogInformation("PumpFunSendMessageEvent:" + JsonConvert.SerializeObject(@event));
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
            Logger.LogInformation("PumpFunSendMessageEvent2:" +
                                  JsonConvert.SerializeObject(@pumpFunSendMessageGEvent));
            await GrainFactory.GetGrain<IPumpFunGrain>(Guid.Parse(@event.ReplyId))
                .SendMessageAsync(@event.ReplyId, @event.ReplyMessage);
            Logger.LogInformation("PumpFunSendMessageEvent3,grainId:" +
                                  GrainFactory.GetGrain<IPumpFunGrain>(Guid.Parse(@event.ReplyId)).GetGrainId());
        }
    }

    public async Task SetPumpFunConfig(string chatId)
    {
        Logger.LogInformation("PumpFunGAgent SetPumpFunConfig, chatId:" + chatId);
        RaiseEvent(new SetPumpFunConfigEvent()
        {
            ChatId = chatId
        });
        await ConfirmEvents();
        Logger.LogInformation("PumpFunGAgent SetPumpFunConfig2, chatId:" + chatId);
    }
}

public interface IPumpFunGAgent : IStateGAgent<PumpFunGAgentState>
{
    Task SetPumpFunConfig(string chatId);
}