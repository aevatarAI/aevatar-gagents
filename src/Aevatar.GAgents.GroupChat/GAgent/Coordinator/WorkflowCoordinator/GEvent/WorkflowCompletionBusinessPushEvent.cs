using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Common;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;

/// <summary>
/// Simplified business event for workflow completion with final result - Inline Implementation
/// </summary>
[GenerateSerializer]
public class WorkflowCompletionBusinessPushEvent : EventBase
{
    /// <summary>
    /// Blackboard ID associated with the workflow
    /// </summary>
    [Id(0)] public Guid BlackboardId { get; set; }
    
    /// <summary>
    /// Unique identifier for the workflow instance
    /// </summary>
    [Id(1)] public string WorkflowId { get; set; } = string.Empty;
    
    /// <summary>
    /// Workflow completion timestamp
    /// </summary>
    [Id(2)] public DateTime CompletionTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The actual result from the final workflow node - this is what business systems need
    /// </summary>
    [Id(3)] public ChatResponse FinalResult { get; set; } = new ChatResponse();
} 