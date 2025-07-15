using System;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.GEvent;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents.TwitterSocialGAgent;

[GenerateSerializer]
public class TwitterSocialGAgentState : GroupMemberState
{
    [Id(1)] public Guid Id { get; set; } = Guid.NewGuid();
    
    // Twitter account info
    [Id(2)] public string UserId { get; set; } = "";
    [Id(3)] public string Token { get; set; } = "";
    [Id(4)] public string TokenSecret { get; set; } = "";
    [Id(5)] public string UserName { get; set; } = "";
    
    // Twitter API configuration
    [Id(6)] public string ConsumerKey { get; set; } = "";
    [Id(7)] public string ConsumerSecret { get; set; } = "";
    [Id(8)] public string EncryptionPassword { get; set; } = "";
    [Id(9)] public string BearerToken { get; set; } = "";
    [Id(10)] public int ReplyLimit { get; set; } = 10;
    
    // Activity tracking
    [Id(11)] public string LastResponse { get; set; } = "";
    [Id(12)] public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
    [Id(13)] public int TotalTweets { get; set; } = 0;
} 