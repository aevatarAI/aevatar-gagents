using Orleans;

namespace AevatarGAgents.Twitter.Agent.GEvents;

[GenerateSerializer]
public class ReceiveTweetReplyGEvent : TweetGEvent
{
    [Id(0)]  public string TweetId { get; set; }
    [Id(1)] public string Text { get; set; }
}