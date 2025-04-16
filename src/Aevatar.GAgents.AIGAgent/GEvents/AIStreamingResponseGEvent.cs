using System;
using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.SignalR;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.GAgents.AIGAgent.GEvents;

[Description("Return a streaming chunk")]
[GenerateSerializer]
public class AIStreamingResponseGEvent : ResponseToPublisherEventBase
{
    [Id(0)] public string ResponseContent { get; set; }
    [Id(1)] public int SerialNumber { get; set; }
    [Id(2)] public AIChatContextDto Context { get; set; } = new();
    [Id(3)] public bool IsLastChunk { get; set; }
}

[Description("Return a error reponse")]
[GenerateSerializer]
public class AIStreamingErrorResponseGEvent : ResponseToPublisherEventBase
{
    [Id(0)] public AIChatContextDto Context { get; set; } = new();
    
    [Id(1)]
    public GrainId GrainId { get; set; }

    [Id(2)]
    public Type HandleExceptionType { get; set; }

    [Id(3)]
    public string ExceptionMessage { get; set; }
}