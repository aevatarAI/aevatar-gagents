using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.AI.Feature.SyncLLMWorker;

[GenerateSerializer]
public class SyncLLMResponseEvent:EventBase
{
    [Id(0)] public Guid RequestId { get; set; }
    [Id(1)] public AIChatContextDto? ContextDto { get; set; } = null;
    [Id(2)] public List<ChatMessage>? ChatResponseList { get; set; }
    [Id(3)] public TokenUsageStatistics? TokenUsageStatistics { get; set; }
    [Id(4)] public string? ErrorMessage { get; set; } = null;
}