using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Common.BasicGEvent.SocialGEvent;


[Description("I can chat with users.")]
[GenerateSerializer]
public class SocialGEvent:EventWithResponseBase<SocialResponseGEvent>
{
    [Description("The content of the chat.")]
    [Id(0)] public string Content { get; set; }
    [Id(1)]  public string MessageId { get; set; }
    [Description("Unique identifier for the chat from which the message was received.")]
    [Id(2)]  public string ChatId { get; set; }
}