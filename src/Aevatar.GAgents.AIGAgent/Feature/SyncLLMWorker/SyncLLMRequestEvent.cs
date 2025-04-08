using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Orleans;

namespace Aevatar.AI.Feature.SyncLLMWorker;

[GenerateSerializer]
public class SyncLLMRequestEvent : EventBase
{
    [Id(0)] public Guid RequestId { get; set; } = Guid.NewGuid();
    [Id(1)] public string GrainId { get; set; }
    [Id(2)] public string Instructions { get; set; }
    [Id(3)] public bool IfUseKnowledge { get; set; }
    [Id(4)] public LLMConfig LLMConfig { get; set; }
    [Id(5)] public string Prompt { get; set; }
    [Id(6)] public List<ChatMessage>? History { get; set; } = null;
    [Id(7)] public ExecutionPromptSettings? PromptSettings { get; set; } = null;
    [Id(8)] public AIChatContextDto? ContextDto { get; set; } = null;
}