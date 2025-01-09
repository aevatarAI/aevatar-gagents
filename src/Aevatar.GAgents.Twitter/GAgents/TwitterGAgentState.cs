using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Twitter.Agent.GEvents;
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
    
    public void Apply(BindTwitterAccountGEvent bindTwitterAccountGEvent)
    {
        UserId = bindTwitterAccountGEvent.UserId;
        Token = bindTwitterAccountGEvent.Token;
        TokenSecret = bindTwitterAccountGEvent.TokenSecret;
        UserName = bindTwitterAccountGEvent.UserName;
    }
    
    public void Apply(UnbindTwitterAccountEvent unbindTwitterAccountEvent)
    {
        Token = "";
        TokenSecret = "";
        UserId = "";
        UserName = "";
    }
    
    public void Apply(ReplyTweetGEvent replyTweetGEvent)
    {
        if (!replyTweetGEvent.TweetId.IsNullOrEmpty())
        {
            RepliedTweets[replyTweetGEvent.TweetId] = replyTweetGEvent.Text;
        }
    }
}