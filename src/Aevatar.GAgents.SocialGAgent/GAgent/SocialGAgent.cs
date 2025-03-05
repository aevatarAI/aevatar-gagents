using Aevatar.GAgents.SocialChat.GAgent;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Aevatar.GAgents.ChatAgent.GAgent.State;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;
using Aevatar.GAgents.SocialAgent.GAgent.SEvent;

namespace Aevatar.GAgents.TestAgent;

[System.ComponentModel.Description("I can chat with users.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(SocialGAgent))]
public class SocialGAgent : ChatGAgentBase<ChatGAgentState, SocialGAgentLogEvent, EventBase, ChatConfigDto>,
    ISocialGAgent
{
    private readonly ILogger<SocialGAgent> _logger;

    public SocialGAgent(ILogger<SocialGAgent> logger, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = logger;
    }

    [EventHandler]
    public async Task<SocialResponseGEvent> HandleEventAsync(SocialGEvent @event)
    {
        _logger.LogInformation("handle SocialEvent, content: {content}", @event.Content);

        SocialResponseGEvent aiResponseEvent = new SocialResponseGEvent();
        aiResponseEvent.RequestId = @event.RequestId;

        try
        {
            var message = await ChatAsync(@event.Content);
            if (message != null && message.Any())
            {
                _logger.LogInformation("handle SocialEvent, AI replyMessage: {msg}", message[0].Content);

                aiResponseEvent.ResponseContent = message[0].Content!;
                aiResponseEvent.ChatId = @event.ChatId;
                aiResponseEvent.ReplyMessageId = @event.MessageId;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle SocialEvent, Get AIReplyMessage Error: {err}", e.Message);
        }

        return aiResponseEvent;
    }
}