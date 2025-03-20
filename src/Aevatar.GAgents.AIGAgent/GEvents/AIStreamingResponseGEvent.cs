using System;
using System.ComponentModel;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.SignalR;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.GEvents;

[Description("Return a streaming chunk")]
[GenerateSerializer]
public class AIStreamingResponseGEvent : ResponseToPublisherEventBase
{
    [Id(0)] public string ResponseContent { get; set; }
    [Id(1)] public int SerialNumber { get; set; }
    [Id(2)] public AIChatContextDto Context { get; set; } = new();
}