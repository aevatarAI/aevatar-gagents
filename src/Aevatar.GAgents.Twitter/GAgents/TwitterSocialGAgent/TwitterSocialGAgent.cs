using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Twitter.Options;
using Aevatar.GAgents.Twitter.Grains;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Aevatar.Core;

namespace Aevatar.GAgents.Twitter.GAgents.TwitterSocialGAgent;

[Description("Simple Twitter agent with AI chat and workflow support - receives messages and tweets directly")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(TwitterSocialGAgent))]
public class TwitterSocialGAgent : GroupMemberGAgentBase<TwitterSocialGAgentState, TwitterSocialGAgentEvent, EventBase, TwitterWorkflowConfigDto>,
    ITwitterSocialGAgent
{
    private readonly ILogger<TwitterSocialGAgent> _logger;

    public TwitterSocialGAgent(ILogger<TwitterSocialGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "A simple Twitter agent that receives messages, processes them with AI, and tweets directly.");
    }

    // GroupMemberGAgentBase implementation
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // High interest when Twitter account is configured
        if (!State.UserName.IsNullOrEmpty() && !State.UserId.IsNullOrEmpty())
        {
            return Task.FromResult(85);
        }
        return Task.FromResult(30);
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        var response = new ChatResponse();
        
        // Check if Twitter account is configured
        if (State.UserId.IsNullOrEmpty())
        {
            var errorMsg = $"{State.MemberName}: Twitter account not configured, cannot tweet";
            _logger.LogWarning(errorMsg);
            response.Content = errorMsg;
            return response;
        }

        string contentToTweet;

        if (messages == null || messages.Count == 0)
        {
            // No messages - use AI to generate content
            _logger.LogInformation($"{State.MemberName} no messages, using AI to generate content");
            var aiMessages = await ChatWithHistory("Generate a tweet");
            contentToTweet = aiMessages?.FirstOrDefault()?.Content ?? 
                           $"{State.MemberName} is active (BlackboardId: {blackboardId.ToString()[..8]})";
        }
        else
        {
            // Has messages - directly tweet the content
            contentToTweet = string.Join(" ", messages.Select(m => m.Content));
            _logger.LogInformation($"{State.MemberName} has messages, tweeting directly: {contentToTweet}");
        }

        // Send tweet
        await SendTweetAsync(contentToTweet);
        
        // Save to state
        RaiseEvent(new TwitterSocialResponseEvent()
        {
            ResponseContent = contentToTweet,
            Timestamp = DateTime.UtcNow
        });
        await ConfirmEvents();
        
        response.Content = $"{State.MemberName} tweeted: {contentToTweet}";
        return response;
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        _logger.LogInformation($"{State.MemberName} workflow finished for blackboard {blackboardId}");
        return Task.CompletedTask;
    }

    // Simple account status check
    public Task<bool> IsTwitterAccountConfiguredAsync()
    {
        return Task.FromResult(!State.UserName.IsNullOrEmpty() && !State.UserId.IsNullOrEmpty());
    }

    // Private helper methods
    private async Task SendTweetAsync(string content)
    {
        try
        {
            await GrainFactory.GetGrain<ITwitterGrain>(State.UserId)
                .CreateTweetAsync(State.ConsumerKey, State.ConsumerSecret, 
                    content, State.Token, State.TokenSecret);
            
            _logger.LogInformation($"Tweet sent successfully: {content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send tweet: {content}");
        }
    }

    protected override async Task PerformConfigAsync(TwitterWorkflowConfigDto configuration)
    {
        // Set MemberName from base
        await base.PerformConfigAsync(configuration);
        
        // Set all configuration including Twitter account info
        RaiseEvent(new TwitterConfigEvent()
        {
            ConsumerKey = configuration.ConsumerKey,
            ConsumerSecret = configuration.ConsumerSecret,
            EncryptionPassword = configuration.EncryptionPassword,
            BearerToken = configuration.BearerToken,
            ReplyLimit = configuration.ReplyLimit,
            // Twitter account info from configuration
            TwitterUserId = configuration.TwitterUserId,
            TwitterUserName = configuration.TwitterUserName,
            TwitterToken = configuration.TwitterToken,
            TwitterTokenSecret = configuration.TwitterTokenSecret
        });
        
        await ConfirmEvents();
        
        _logger.LogInformation($"TwitterSocialGAgent configured for user: {configuration.TwitterUserName}");
    }

    protected override void GroupMemberTransitionState(TwitterSocialGAgentState state,
        StateLogEventBase<TwitterSocialGAgentEvent> @event)
    {
        _logger.LogDebug("GroupMemberTransitionState: {data}, type:{type}",
            JsonConvert.SerializeObject(@event), @event.GetType().FullName);
        
        switch (@event)
        {
            case TwitterConfigEvent configEvent:
                // API configuration
                State.ConsumerKey = configEvent.ConsumerKey;
                State.ConsumerSecret = configEvent.ConsumerSecret;
                State.EncryptionPassword = configEvent.EncryptionPassword;
                State.BearerToken = configEvent.BearerToken;
                State.ReplyLimit = configEvent.ReplyLimit;
                
                // Twitter account info
                State.UserId = configEvent.TwitterUserId;
                State.UserName = configEvent.TwitterUserName;
                State.Token = configEvent.TwitterToken;
                State.TokenSecret = configEvent.TwitterTokenSecret;
                break;
                
            case TwitterSocialResponseEvent responseEvent:
                State.LastResponse = responseEvent.ResponseContent;
                State.LastActivityTime = responseEvent.Timestamp;
                State.TotalTweets++;
                break;
        }
    }
}

// Simplified interface
public interface ITwitterSocialGAgent : IStateGAgent<TwitterSocialGAgentState>
{
    Task<bool> IsTwitterAccountConfiguredAsync();
} 