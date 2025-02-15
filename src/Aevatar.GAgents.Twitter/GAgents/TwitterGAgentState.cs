using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Twitter.Agent.GEvents;
using Aevatar.GAgents.Twitter.Options;
using Orleans;

namespace Aevatar.GAgents.Twitter.Agent;

[GenerateSerializer]
public class TwitterGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
    [Id(1)] public string UserId { get; set; }
    [Id(2)] public string Token { get; set; }
    [Id(3)] public string TokenSecret { get; set; }
    [Id(4)] public Dictionary<string, string> RepliedTweets { get; set; }
    [Id(5)] public string UserName { get; set; }
    [Id(6)] public List<Guid> SocialRequestList { get; set; } = new List<Guid>();
    [Id(7)] public InitTwitterOptionsDto TwitterOptions { get; set; }

    public void Apply(BindTwitterAccountSEvent bindTwitterAccountSEvent)
    {
        UserId = bindTwitterAccountSEvent.UserId;
        Token = bindTwitterAccountSEvent.Token;
        TokenSecret = bindTwitterAccountSEvent.TokenSecret;
        UserName = bindTwitterAccountSEvent.UserName;
    }

    public void Apply(UnbindTwitterAccountEvent unbindTwitterAccountEvent)
    {
        Token = "";
        TokenSecret = "";
        UserId = "";
        UserName = "";
    }

    public void Apply(ReplyTweetSEvent replyTweetSEvent)
    {
        if (!replyTweetSEvent.TweetId.IsNullOrEmpty())
        {
            RepliedTweets[replyTweetSEvent.TweetId] = replyTweetSEvent.Text;
        }
    }

    public void Apply(TweetRequestSEvent @event)
    {
        if (SocialRequestList.Contains(@event.RequestId) == false)
        {
            SocialRequestList.Add(@event.RequestId);
        }
    }

    public void Apply(TweetSocialResponseSEvent @event)
    {
        if (SocialRequestList.Contains(@event.ResponseId))
        {
            SocialRequestList.Remove(@event.ResponseId);
        }
    }

    public void Apply(TwitterOptionsSEvent @event)
    {
        TwitterOptions = new InitTwitterOptionsDto()
        {
            ConsumerKey = @event.ConsumerKey,
            ConsumerSecret = @event.ConsumerSecret,
            EncryptionPassword = @event.EncryptionPassword,
            BearerToken = @event.BearerToken,
            ReplyLimit = @event.ReplyLimit,
        };
    }
}