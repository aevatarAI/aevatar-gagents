using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgent.NamingContest.TrafficGAgent;

[Description("tell group to start")]
[GenerateSerializer]
public class GroupStartGEvent : EventBase
{
    [Description("Unique identifier for the  message.")]
    [Id(0)]  public string MessageId { get; set; }
    
    [Description("Text content of the  message.")]
    [Id(1)] public string Message { get; set; }
}