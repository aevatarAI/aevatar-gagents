using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Router.Demo.GEvents;

[Description("Post a tweet")]
[GenerateSerializer]
public class PostTweetGEvent : EventBase
{
    [Description("content to be post")]
    [Id(0)]  public string Text { get; set; }
}