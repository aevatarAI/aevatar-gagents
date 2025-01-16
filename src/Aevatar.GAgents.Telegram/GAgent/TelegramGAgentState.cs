using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using AISmart.GAgent.Telegram.Agent.GEvents;
using Orleans;

namespace AISmart.GAgent.Telegram.Agent;

public class TelegramGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Id(1)] public Dictionary<string, ReceiveMessageSEvent> PendingMessages { get; set; } = new Dictionary<string, ReceiveMessageSEvent>();
    
    [Id(3)] public string BotName { get; set; } 
    
    [Id(4)] public string Token { get; set; } 
    [Id(5)] public List<Guid> SocialRequestList { get; set; } = new List<Guid>();
    
    public void Apply(ReceiveMessageSEvent receiveMessageSEvent)
    {
        PendingMessages[receiveMessageSEvent.MessageId] = receiveMessageSEvent;
    }
    
    public void Apply(SendMessageSEvent sendMessageSEvent)
    {
        if (!sendMessageSEvent.ReplyMessageId.IsNullOrEmpty())
        {
            PendingMessages.Remove(sendMessageSEvent.ReplyMessageId);
        }
    }
    
    public void Apply(SetTelegramConfigEvent setTelegramConfigEvent)
    {
        BotName = setTelegramConfigEvent.BotName;
        Token = setTelegramConfigEvent.Token;
    }

    public void Apply(TelegramRequestSEvent @event)
    {
        if (SocialRequestList.Contains(@event.RequestId) == false)
        {
            SocialRequestList.Add(@event.RequestId);
        }
    }

    public void Apply(TelegramSocialResponseSEvent @event)
    {
        if (SocialRequestList.Contains(@event.ResponseId))
        {
            SocialRequestList.Remove(@event.ResponseId);
        }
    }
}