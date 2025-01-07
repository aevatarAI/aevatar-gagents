using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.Twitter.GEvents;

[Description("reply mention in tweet.")]
[GenerateSerializer]
public class ReplyMentionGEvent: EventBase
{
    
}