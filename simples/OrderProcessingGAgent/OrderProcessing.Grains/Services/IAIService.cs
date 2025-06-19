using System;
using System.Threading.Tasks;
using OrderProcessing.Grains.Dto;

namespace OrderProcessing.Grains.Services;

public interface IAIService
{
    /// <summary>
    /// AI智能分析订单风险
    /// </summary>
    Task<OrderRiskAnalysis> AnalyzeOrderRiskAsync(OrderDto order);
    
    /// <summary>
    /// AI智能决策工作流路径
    /// </summary>
    Task<WorkflowDecision> MakeWorkflowDecisionAsync(string stepName, OrderDto order, OrderRiskAnalysis riskAnalysis);
    
    /// <summary>
    /// AI agent间智能对话
    /// </summary>
    Task<AgentResponse> GenerateAgentResponseAsync(string agentRole, string message, OrderDto order);
    
    /// <summary>
    /// AI智能建议生成
    /// </summary>
    Task<List<string>> GenerateSmartSuggestionsAsync(OrderDto order, string context);
}

[GenerateSerializer]
public class OrderRiskAnalysis
{
    [Id(0)]
    public int RiskScore { get; set; } // 0-100
    
    [Id(1)]
    public List<string> RiskFactors { get; set; } = new();
    
    [Id(2)]
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High, Critical
    
    [Id(3)]
    public List<string> Recommendations { get; set; } = new();
    
    [Id(4)]
    public bool RequiresHumanReview { get; set; }
}

[GenerateSerializer]
public class WorkflowDecision
{
    [Id(0)]
    public string RecommendedAction { get; set; } = string.Empty; // Approve, Reject, Review, Escalate
    
    [Id(1)]
    public string NextStep { get; set; } = string.Empty;
    
    [Id(2)]
    public int ConfidenceLevel { get; set; } // 0-100
    
    [Id(3)]
    public string Reasoning { get; set; } = string.Empty;
    
    [Id(4)]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

[GenerateSerializer]
public class AgentResponse
{
    [Id(0)]
    public string Message { get; set; } = string.Empty;
    
    [Id(1)]
    public string Intent { get; set; } = string.Empty;
    
    [Id(2)]
    public Dictionary<string, object> Data { get; set; } = new();
    
    [Id(3)]
    public bool RequiresResponse { get; set; }
} 