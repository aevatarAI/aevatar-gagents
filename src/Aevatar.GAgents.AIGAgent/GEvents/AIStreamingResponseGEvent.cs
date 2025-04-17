using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.GEvents;

[Description("Return a streaming chunk")]
[GenerateSerializer]
public class AIStreamingResponseGEvent : EventBase
{
    [Id(0)] public string ResponseContent { get; set; }
    [Id(1)] public int SerialNumber { get; set; }
    [Id(2)] public AIChatContextDto Context { get; set; } = new();
    [Id(3)] public bool IsLastChunk { get; set; }
    [Id(4)] public bool IfAggregationMsg { get; set; }
}