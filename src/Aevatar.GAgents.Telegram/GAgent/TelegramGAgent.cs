using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;
using Aevatar.GAgents.Telegram.Agent.GEvents;
using Aevatar.GAgents.Telegram.GEvents;
using Aevatar.GAgents.Telegram.Grains;
using Aevatar.GAgents.Telegram.Options;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.GAgents.Telegram.Agent;

[Description("Handle telegram")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(TelegramGAgent))]
public class TelegramGAgent : GAgentBase<TelegramGAgentState, MessageSEvent, EventBase, TelegramOptionsDto>,
    ITelegramGAgent
{
    public TelegramGAgent(ILogger<TelegramGAgent> logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for informing other agents when a Telegram thread is published.");
    }

    public async Task RegisterTelegramAsync(string botName, string token)
    {
        RaiseEvent(new SetTelegramConfigEvent()
        {
            BotName = botName,
            Token = token
        });
        await ConfirmEvents();
        await GrainFactory.GetGrain<ITelegramGrain>(botName).RegisterTelegramAsync(State.TelegramOptions.Webhook,
            State.BotName, State.Token);
    }

    public async Task UnRegisterTelegramAsync(string botName)
    {
        await GrainFactory.GetGrain<ITelegramGrain>(botName).UnRegisterTelegramAsync(
            State.Token);
    }


    [EventHandler]
    public async Task HandleEventAsync(ReceiveMessageGEvent @event)
    {
        Logger.LogInformation("Telegram ReceiveMessageEvent " + @event.MessageId);
        if (State.PendingMessages.TryGetValue(@event.MessageId, out _))
        {
            Logger.LogDebug("Message reception repeated for Telegram Message ID: " + @event.MessageId);
            return;
        }

        var requestId = Guid.NewGuid();
        RaiseEvent(new TelegramRequestSEvent() { RequestId = requestId });
        await ConfirmEvents();

        RaiseEvent(new ReceiveMessageSEvent
        {
            MessageId = @event.MessageId,
            ChatId = @event.ChatId,
            Message = @event.Message,
            NeedReplyBotName = State.BotName
        });
        await ConfirmEvents();
        await PublishAsync(new SocialGEvent()
        {
            Content = @event.Message,
            MessageId = @event.MessageId,
            ChatId = @event.ChatId
        });
        Logger.LogDebug("Publish AutoGenCreatedEvent for Telegram Message ID: " + @event.MessageId);
    }

    [EventHandler]
    public async Task HandleEventAsync(SendMessageGEvent @event)
    {
        Logger.LogDebug("Publish SendMessageEvent for Telegram Message: " + @event.Message);
        await SendMessageAsync(@event.Message, @event.ChatId, @event.ReplyMessageId);
    }

    [EventHandler]
    public async Task HandleEventAsync(SocialResponseGEvent @event)
    {
        if (@event.RequestId != Guid.Empty)
        {
            if (State.SocialRequestList.Contains(@event.RequestId))
            {
                RaiseEvent(new TelegramSocialResponseSEvent() { ResponseId = @event.RequestId });
                await ConfirmEvents();
            }
            else
            {
                return;
            }
        }

        Logger.LogDebug("SocialResponse for Telegram Message: " + @event.ResponseContent);
        await SendMessageAsync(@event.ResponseContent, @event.ChatId, @event.ReplyMessageId);
    }

    private async Task SendMessageAsync(string message, string chatId, string? replyMessageId)
    {
        if (replyMessageId != null)
        {
            RaiseEvent(new SendMessageSEvent()
            {
                ReplyMessageId = replyMessageId,
                ChatId = chatId,
                Message = message
            });
            await ConfirmEvents();
        }

        await GrainFactory.GetGrain<ITelegramGrain>(State.BotName).SendMessageAsync(
            State.Token, chatId, message, replyMessageId);
    }

    protected override async Task PerformConfigAsync(TelegramOptionsDto initializationEvent)
    {
        RaiseEvent(new TelegramOptionSEvent()
            { Webhook = initializationEvent.Webhook, EncryptionPassword = initializationEvent.EncryptionPassword });

        await ConfirmEvents();
    }
}

public interface ITelegramGAgent : IStateGAgent<TelegramGAgentState>
{
    Task RegisterTelegramAsync(string botName, string token);

    Task UnRegisterTelegramAsync(string botName);
}