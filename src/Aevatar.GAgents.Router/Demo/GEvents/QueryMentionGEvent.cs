using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Router.Demo.GEvents;

[Description("query mention tweet")]
[GenerateSerializer]
public class QueryMentionGEvent : EventBase
{
    [Description("user id to be queried")]
    [Id(0)]  public string UserID { get; set; }
}