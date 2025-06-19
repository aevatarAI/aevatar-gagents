using System;
using Aevatar.Core.Abstractions;
using OrderProcessing.Grains.Dto;
using OrderProcessing.Grains.Services;

namespace OrderProcessing.Grains.Events;

[GenerateSerializer]
public class AgentConversationEvent : EventBase
{
    [Id(0)]
    public string FromAgentId { get; set; } = string.Empty;
    
    [Id(1)]
    public string ToAgentId { get; set; } = string.Empty;
    
    [Id(2)]
    public string Message { get; set; } = string.Empty;
    
    [Id(3)]
    public string ConversationType { get; set; } = string.Empty; // Request, Response, Notification
    
    [Id(4)]
    public Guid OrderId { get; set; }
    
    [Id(5)]
    public Dictionary<string, object> ConversationData { get; set; } = new();
    
    [Id(6)]
    public DateTime ConversationTime { get; set; } = DateTime.UtcNow;
}

[GenerateSerializer]
public class AIAnalysisRequestEvent : EventBase
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public OrderDto Order { get; set; } = new();
    
    [Id(2)]
    public string RequestingAgentId { get; set; } = string.Empty;
    
    [Id(3)]
    public string AnalysisType { get; set; } = string.Empty; // Risk, Workflow, General
    
    [Id(4)]
    public string Context { get; set; } = string.Empty;
}

[GenerateSerializer]
public class AIAnalysisResponseEvent : EventBase
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public string RequestingAgentId { get; set; } = string.Empty;
    
    [Id(2)]
    public string RespondingAgentId { get; set; } = string.Empty;
    
    [Id(3)]
    public OrderRiskAnalysis RiskAnalysis { get; set; } = new();
    
    [Id(4)]
    public WorkflowDecision WorkflowDecision { get; set; } = new();
    
    [Id(5)]
    public List<string> Suggestions { get; set; } = new();
    
    [Id(6)]
    public string AnalysisMessage { get; set; } = string.Empty;
}

[GenerateSerializer]
public class AIDecisionConsultationEvent : EventBase
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public string ConsultingAgentId { get; set; } = string.Empty;
    
    [Id(2)]
    public string TargetAgentId { get; set; } = string.Empty;
    
    [Id(3)]
    public string DecisionContext { get; set; } = string.Empty;
    
    [Id(4)]
    public WorkflowDecision ProposedDecision { get; set; } = new();
    
    [Id(5)]
    public string ConsultationQuestion { get; set; } = string.Empty;
}

[GenerateSerializer]
public class AIDecisionResponseEvent : EventBase
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public string ConsultingAgentId { get; set; } = string.Empty;
    
    [Id(2)]
    public string RespondingAgentId { get; set; } = string.Empty;
    
    [Id(3)]
    public bool AgreesWithDecision { get; set; }
    
    [Id(4)]
    public WorkflowDecision AlternativeDecision { get; set; } = new();
    
    [Id(5)]
    public string ResponseMessage { get; set; } = string.Empty;
    
    [Id(6)]
    public int ConsensusScore { get; set; } // 0-100
}

[GenerateSerializer]
public class SmartWorkflowAdjustmentEvent : EventBase
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public string AdjustingAgentId { get; set; } = string.Empty;
    
    [Id(2)]
    public string OriginalStep { get; set; } = string.Empty;
    
    [Id(3)]
    public string NewStep { get; set; } = string.Empty;
    
    [Id(4)]
    public string AdjustmentReason { get; set; } = string.Empty;
    
    [Id(5)]
    public WorkflowDecision AIDecision { get; set; } = new();
} 