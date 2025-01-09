using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.Agent.GEvents;

public class TweetGEvent : GEventBase
{
    [Id(0)] public string Text { get; set; }
}