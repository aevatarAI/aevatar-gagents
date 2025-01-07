using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.Twitter.GEvents;

[Description("create a tweet")]
[GenerateSerializer]
public class CreateTweetGEvent:EventBase
{
    [Description("text content to be post")]
    [Id(0)]  public string Text { get; set; }
}