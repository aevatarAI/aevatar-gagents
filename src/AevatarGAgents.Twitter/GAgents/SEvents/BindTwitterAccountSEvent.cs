using Orleans;

namespace AevatarGAgents.Twitter.Agent.GEvents;

[GenerateSerializer]
public class BindTwitterAccountGEvent : TweetGEvent
{
    [Id(0)] public string UserId { get; set; }
    [Id(1)] public string Token { get; set; }
    [Id(2)] public string TokenSecret { get; set; }
    [Id(3)] public string UserName { get; set; }
}