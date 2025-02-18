
using Aevatar.GAgents.SocialChat.GAgent;
using Json.Schema.Generation;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;
using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;

namespace Aevatar.GAgents.TestAgent;

[System.ComponentModel.Description("I can chat with users.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(SocialGAgent))]
public class SocialGAgent : MicroAIGAgent, ISocialGAgent
{
    [EventHandler]
    public  async Task<SocialResponseGEvent> HandleEventAsync(SocialGEvent @event)
    {
        Logger.LogInformation("handle SocialEvent, content: {content}", @event.Content);
        List<AIMessageStateLogEvent> list = new List<AIMessageStateLogEvent>();
        list.Add(new AiReceiveStateLogEvent
        {
            Message = new MicroAIMessage("user", @event.Content)
        });
        
        SocialResponseGEvent aiResponseEvent = new SocialResponseGEvent();
        aiResponseEvent.RequestId = @event.RequestId;
        
        try
        {
            var message = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(@event.Content, State.RecentMessages.ToList());
            if (message != null && !message.Content.IsNullOrEmpty())
            {
                Logger.LogInformation("handle SocialEvent, AI replyMessage: {msg}", message.Content);
                list.Add(new AiReplyStateLogEvent()
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
            Logger.LogError(e, "handle SocialEvent, Get AIReplyMessage Error: {err}", e.Message);
        }
        
        base.RaiseEvents(list);
        return aiResponseEvent;
    }
}