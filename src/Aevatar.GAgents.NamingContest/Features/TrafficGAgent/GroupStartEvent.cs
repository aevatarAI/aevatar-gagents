using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.NamingContest.TrafficGAgent;

[Description("tell group to start")]
[GenerateSerializer]
public class GroupStartEvent : EventBase
{
    [Description("Unique identifier for the  message.")]
    [Id(0)]  public string MessageId { get; set; }
    
    [Description("Text content of the  message.")]
    [Id(1)] public string Message { get; set; }
}