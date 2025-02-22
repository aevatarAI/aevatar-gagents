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
    [Id(7)] public InitTwitterOptions TwitterOptions { get; set; }
}



[GenerateSerializer]
public class InitTwitterOptions : ConfigurationBase
{
    [Id(0)] public string ConsumerKey { get; set; }
    [Id(1)] public string ConsumerSecret { get; set; }
    [Id(2)] public string EncryptionPassword { get; set; }
    [Id(3)] public string BearerToken { get; set; }
    [Id(4)] public int ReplyLimit { get; set; }
}