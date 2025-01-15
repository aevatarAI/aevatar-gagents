using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.MicroAI.Grains;
using Aevatar.GAgents.SocialChat.GAgent;
using Json.Schema.Generation;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;

namespace Aevatar.GAgents.TestAgent;

[Description("I can chat with users.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class SocialGAgent : MicroAIGAgent, ISocialGAgent
{
    public SocialGAgent(ILogger<MicroAIGAgent> logger) : base(logger)
    {
    }

    [EventHandler]
    public  async Task<SocialResponseGEvent> HandleEventAsync(SocialGEvent @event)
    {
        _logger.LogInformation("handle SocialEvent, content: {content}", @event.Content);
        List<AIMessageGEvent> list = new List<AIMessageGEvent>();
        list.Add(new AIReceiveMessageGEvent
        {
            Message = new MicroAIMessage("user", @event.Content)
        });
        
        SocialResponseGEvent aiResponseEvent = new SocialResponseGEvent();
        try
        {
            var message = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(@event.Content, State.RecentMessages.ToList());
            if (message != null && !message.Content.IsNullOrEmpty())
            {
                _logger.LogInformation("handle SocialEvent, AI replyMessage: {msg}", message.Content);
                list.Add(new AIReplyMessageGEvent()
                {
                    Message = message
                });

                aiResponseEvent.ResponseContent = message.Content;
                aiResponseEvent.ChatId = @event.ChatId;
                aiResponseEvent.ReplyMessageId = @event.MessageId;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle SocialEvent, Get AIReplyMessage Error: {err}", e.Message);
        }
        
        base.RaiseEvents(list);
        return aiResponseEvent;
    }
}