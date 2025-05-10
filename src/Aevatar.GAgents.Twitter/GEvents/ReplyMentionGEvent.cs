using System.Collections.Generic;
using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.GEvents;

[Description("reply mention in tweet.")]
[GenerateSerializer]
public class ReplyMentionGEvent: EventBase
{
    
}

[Description("reply mention in tweet exclude authorIds .")]
[GenerateSerializer]
public class ReplyMentionExcludeAuthorIdsGEvent: EventBase
{
    [Id(0)] public List<string> ExcludeAuthorIds { get; set; } = new List<string>();
}