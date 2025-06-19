using Aevatar.Core.Abstractions;

namespace SimpleAIWorkflow.Grains.Events;

/// <summary>
/// Begin workflow task event
/// </summary>
public class BeginWorkflowTaskEvent : EventBase
{
    public string TaskDescription { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Workflow step completed event
/// </summary>
public class WorkflowStepCompletedEvent : EventBase
{
    public string StepName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string NextStepName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; } = true;
}

/// <summary>
/// Data validation event
/// </summary>
public class DataValidationEvent : EventBase
{
    public Dictionary<string, object> Data { get; set; } = new();
    public string ValidationRules { get; set; } = string.Empty;
}

/// <summary>
/// Risk analysis event
/// </summary>
public class RiskAnalysisEvent : EventBase
{
    public Dictionary<string, object> BusinessData { get; set; } = new();
    public string AnalysisType { get; set; } = string.Empty;
}

/// <summary>
/// Approval decision event
/// </summary>
public class ApprovalDecisionEvent : EventBase
{
    public string RequestId { get; set; } = string.Empty;
    public Dictionary<string, object> ApprovalData { get; set; } = new();
    public string DecisionReason { get; set; } = string.Empty;
}

/// <summary>
/// Business processing event
/// </summary>
public class BusinessProcessingEvent : EventBase
{
    public string ProcessType { get; set; } = string.Empty;
    public Dictionary<string, object> ProcessData { get; set; } = new();
    public string Instructions { get; set; } = string.Empty;
} 