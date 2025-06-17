using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;

[GenerateSerializer]
public class WorkflowCompletionBusinessPushEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public string WorkflowId { get; set; } = string.Empty;
    [Id(2)] public DateTime CompletionTime { get; set; } = DateTime.UtcNow;
    [Id(3)] public List<string> ParticipantGrainIds { get; set; } = new List<string>();
    [Id(4)] public Dictionary<string, object> WorkflowResults { get; set; } = new Dictionary<string, object>();
    [Id(5)] public string BusinessContext { get; set; } = string.Empty;
} 