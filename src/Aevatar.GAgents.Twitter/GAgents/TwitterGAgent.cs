using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.GAgents.Twitter.Agent.GEvents;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;
using Aevatar.GAgents.Twitter.GEvents;
using Aevatar.GAgents.Twitter.Grains;
using Aevatar.GAgents.Twitter.Options;
using Newtonsoft.Json;

namespace Aevatar.GAgents.Twitter.Agent;

[Description("Handle telegram")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(TwitterGAgent))]
public class TwitterGAgent : GAgentBase<TwitterGAgentState, TweetSEvent, EventBase, InitTwitterOptionsDto>,
    ITwitterGAgent
{
    private readonly ILogger<TwitterGAgent> _logger;

    public TwitterGAgent(ILogger<TwitterGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for informing other agents when a twitter thread is published.");
    }

    [EventHandler]
    public async Task HandleEventAsync(ReceiveReplyGEvent @event)
    {
        _logger.LogInformation("HandleEventAsync ReceiveReplyEvent, id: {id} ", @event.TweetId);

        var requestId = Guid.NewGuid();
        RaiseEvent(new TweetRequestSEvent() { RequestId = requestId });
        await ConfirmEvents();

        await PublishAsync(new SocialGEvent()
        {
            RequestId = requestId,
            Content = @event.Text,
            MessageId = @event.TweetId
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(CreateTweetGEvent @event)
    {
        _logger.LogInformation("HandleEventAsync CreateTweetEvent, text: {text}", @event.Text);
        if (@event.Text.IsNullOrEmpty())
        {
            return;
        }

        if (State.UserId.IsNullOrEmpty())
        {
            _logger.LogInformation("HandleEventAsync SocialResponseEvent null userId");
            return;
        }

        var requestId = Guid.NewGuid();
        RaiseEvent(new TweetRequestSEvent() { RequestId = requestId });
        await ConfirmEvents();

        await PublishAsync(new SocialGEvent()
        {
            RequestId = requestId,
            Content = @event.Text
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(SocialResponseGEvent @event)
    {
        if (@event.RequestId != Guid.Empty)
        {
            if (State.SocialRequestList.Contains(@event.RequestId))
            {
                RaiseEvent(new TweetSocialResponseSEvent() { ResponseId = @event.RequestId });
                await ConfirmEvents();
            }
            else
            {
                return;
            }
        }

        _logger.LogInformation("HandleEventAsync SocialResponseEvent, content: {text}, id: {id}",
            @event.ResponseContent, @event.ReplyMessageId);
        if (State.UserId.IsNullOrEmpty())
        {
            _logger.LogInformation("HandleEventAsync SocialResponseEvent null userId");
            return;
        }

        if (@event.ReplyMessageId.IsNullOrEmpty())
        {
            await GrainFactory.GetGrain<ITwitterGrain>(State.UserId).CreateTweetAsync(State.ConsumerKey,
                State.ConsumerSecret,
                @event.ResponseContent, State.Token, State.TokenSecret);
        }
        else
        {
            RaiseEvent(new ReplyTweetSEvent
            {
                TweetId = @event.ReplyMessageId,
                Text = @event.ResponseContent
            });
            await ConfirmEvents();

            await GrainFactory.GetGrain<ITwitterGrain>(State.UserId).ReplyTweetAsync(State.ConsumerKey,
                State.ConsumerSecret,
                @event.ResponseContent, @event.ReplyMessageId, State.Token, State.TokenSecret);
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(ReplyMentionGEvent @event)
    {
        try
        {
            _logger.LogDebug("HandleEventAsync ReplyMentionEvent");
            if (State.UserId.IsNullOrEmpty())
            {
                _logger.LogDebug("HandleEventAsync ReplyMentionEvent null userId");
                return;
            }

            var mentionTweets =
                await GrainFactory.GetGrain<ITwitterGrain>(State.UserId)
                    .GetRecentMentionAsync(State.UserName, State.BearerToken,
                        State.ReplyLimit);
            foreach (var tweet in mentionTweets)
            {
                if (!State.RepliedTweets.Keys.Contains(tweet.Id))
                {
                    var requestId = Guid.NewGuid();
                    RaiseEvent(new TweetRequestSEvent() { RequestId = requestId });
                    await ConfirmEvents();

                    await PublishAsync(new SocialGEvent()
                    {
                        RequestId = requestId,
                        Content = tweet.Text,
                        MessageId = tweet.Id
                    });
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"[TwitterGAgent][ReplyMentionGEvent] handle error:{e}");
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(BindTwitterAccountGEvent @event)
    {
        await BindTwitterAccountAsync(@event.UserName, @event.UserId, @event.Token, @event.TokenSecret);
    }

    [EventHandler]
    public async Task HandleEventAsync(UnbindTwitterAccountGEvent @event)
    {
        await UnbindTwitterAccountAsync();
    }

    public async Task BindTwitterAccountAsync(string userName, string userId, string token, string tokenSecret)
    {
        _logger.LogDebug("HandleEventAsync BindTwitterAccount，userId: {userId}, userName: {userName}",
            userId, userName);
        RaiseEvent(new BindTwitterAccountSEvent()
        {
            UserId = userId,
            Token = token,
            TokenSecret = tokenSecret,
            UserName = userName
        });
        await ConfirmEvents();
    }

    public async Task UnbindTwitterAccountAsync()
    {
        _logger.LogDebug("HandleEventAsync UnbindTwitterAccount，userId: {userId}", State.UserId);
        RaiseEvent(new UnbindTwitterAccountEvent()
        {
        });
        await ConfirmEvents();
    }

    public Task<bool> UserHasBoundAsync()
    {
        return Task.FromResult(!State.UserName.IsNullOrEmpty());
    }

    protected override async Task PerformConfigAsync(InitTwitterOptionsDto initializationEvent)
    {
        _logger.LogDebug("PerformConfigAsync , data: {data}",
            JsonConvert.SerializeObject(initializationEvent));
        RaiseEvent(new TwitterOptionsSEvent()
        {
            ConsumerKey = initializationEvent.ConsumerKey,
            ConsumerSecret = initializationEvent.ConsumerSecret,
            EncryptionPassword = initializationEvent.EncryptionPassword,
            BearerToken = initializationEvent.BearerToken,
            ReplyLimit = initializationEvent.ReplyLimit,
        });

        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(TwitterGAgentState state,
        StateLogEventBase<TweetSEvent> @event)
    {
        _logger.LogDebug("PerformConfigAsync, GAgentTransitionState: {data}, type:{type}",
            JsonConvert.SerializeObject(@event), @event.GetType().FullName);
        switch (@event)
        {
            case TwitterOptionsSEvent twitterOptionsSEvent:
                State.ConsumerKey = twitterOptionsSEvent.ConsumerKey;
                State.ConsumerSecret = twitterOptionsSEvent.ConsumerSecret;
                State.EncryptionPassword = twitterOptionsSEvent.EncryptionPassword;
                State.BearerToken = twitterOptionsSEvent.BearerToken;
                State.ReplyLimit = twitterOptionsSEvent.ReplyLimit;
                break;
            case BindTwitterAccountSEvent bindTwitterAccountSEvent:
                State.UserId = bindTwitterAccountSEvent.UserId;
                State.Token = bindTwitterAccountSEvent.Token;
                State.TokenSecret = bindTwitterAccountSEvent.TokenSecret;
                State.UserName = bindTwitterAccountSEvent.UserName;
                break;
            case UnbindTwitterAccountEvent unbindTwitterAccountEvent:
                State.Token = "";
                State.TokenSecret = "";
                State.UserId = "";
                State.UserName = "";
                break;
            case ReplyTweetSEvent replyTweetSEvent:
                if (!replyTweetSEvent.TweetId.IsNullOrEmpty())
                {
                    State.RepliedTweets[replyTweetSEvent.TweetId] = replyTweetSEvent.Text;
                }

                break;
            case TweetRequestSEvent tweetRequestSEvent:
                if (State.SocialRequestList.Contains(tweetRequestSEvent.RequestId) == false)
                {
                    State.SocialRequestList.Add(tweetRequestSEvent.RequestId);
                }

                break;
            case TweetSocialResponseSEvent tweetSocialResponseSEvent:
                if (State.SocialRequestList.Contains(tweetSocialResponseSEvent.ResponseId))
                {
                    State.SocialRequestList.Remove(tweetSocialResponseSEvent.ResponseId);
                }

                break;
        }
    }
}

public interface ITwitterGAgent : IStateGAgent<TwitterGAgentState>
{
    Task BindTwitterAccountAsync(string userName, string userId, string token, string tokenSecret);
    Task UnbindTwitterAccountAsync();
    Task<bool> UserHasBoundAsync();
}