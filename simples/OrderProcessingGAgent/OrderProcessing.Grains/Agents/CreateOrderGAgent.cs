using System;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using OrderProcessing.Grains.Dto;
using OrderProcessing.Grains.Events;
using OrderProcessing.Grains.Services;

namespace OrderProcessing.Grains.Agents;

public interface ICreateOrderGAgent : IGAgent
{
    Task<OrderDto> CreateOrderAsync(string customerName, string productName, decimal amount, int quantity);
    Task InitializeWorkflowAsync(OrderDto order);
    Task<OrderRiskAnalysis> AnalyzeOrderRiskAsync(OrderDto order);
    Task RequestWorkflowConsultationAsync(OrderDto order, WorkflowDecision proposedDecision);
}

[GenerateSerializer]
public class CreateOrderState : StateBase
{
    [Id(0)]
    public Dictionary<Guid, OrderDto> CreatedOrders { get; set; } = new();
    
    [Id(1)]
    public DateTime LastOperationTime { get; set; }
}

[GenerateSerializer]
public class CreateOrderEventLog : StateLogEventBase<CreateOrderEventLog>
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public string Operation { get; set; } = string.Empty;
}

[GAgent(nameof(CreateOrderGAgent))]
public class CreateOrderGAgent : GAgentBase<CreateOrderState, CreateOrderEventLog>, ICreateOrderGAgent
{
    private readonly IAIService _aiService;

    public CreateOrderGAgent(IAIService aiService)
    {
        _aiService = aiService;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("🤖 AI-Powered Order Analysis Agent - 智能订单分析师，专注于风险评估、异常检测和智能决策支持");
    }

    public async Task<OrderDto> CreateOrderAsync(string customerName, string productName, decimal amount, int quantity)
    {
        Logger.LogInformation("🤖 AI Order Analyst: Creating and analyzing new order for customer: {CustomerName}", customerName);
        
        var order = new OrderDto
        {
            OrderId = Guid.NewGuid(),
            CustomerName = customerName,
            ProductName = productName,
            Amount = amount,
            Quantity = quantity,
            CreateTime = DateTime.UtcNow,
            Status = OrderStatus.Created
        };

        // AI智能风险分析
        var riskAnalysis = await AnalyzeOrderRiskAsync(order);
        
        // 记录事件日志
        RaiseEvent(new CreateOrderEventLog
        {
            OrderId = order.OrderId,
            Operation = "OrderCreated_WithAIAnalysis"
        });
        await ConfirmEvents();

        // 发布订单创建事件
        await PublishAsync(new OrderCreatedEvent
        {
            Order = order
        });

        // 发布AI分析结果
        await PublishAsync(new AIAnalysisResponseEvent
        {
            OrderId = order.OrderId,
            RespondingAgentId = "OrderAnalyst",
            RiskAnalysis = riskAnalysis,
            AnalysisMessage = $"AI风险分析完成：风险等级{riskAnalysis.RiskLevel}，评分{riskAnalysis.RiskScore}/100"
        });

        // AI生成智能建议
        var suggestions = await _aiService.GenerateSmartSuggestionsAsync(order, "creation");
        
        // 发送AI对话消息给协调员
        var aiResponse = await _aiService.GenerateAgentResponseAsync("OrderAnalyst", 
            $"新订单{order.OrderId}已创建并完成智能分析，风险等级为{riskAnalysis.RiskLevel}。建议下一步行动.", order);
            
        await PublishAsync(new AgentConversationEvent
        {
            FromAgentId = "OrderAnalyst",
            ToAgentId = "WorkflowCoordinator", 
            Message = aiResponse.Message,
            ConversationType = "Notification",
            OrderId = order.OrderId,
            ConversationData = new Dictionary<string, object>
            {
                ["riskScore"] = riskAnalysis.RiskScore,
                ["riskLevel"] = riskAnalysis.RiskLevel,
                ["suggestions"] = suggestions
            }
        });

        Logger.LogInformation("🤖 AI Order created with risk analysis: {OrderId}, Risk: {RiskLevel}", 
            order.OrderId, riskAnalysis.RiskLevel);
        return order;
    }

    public async Task InitializeWorkflowAsync(OrderDto order)
    {
        Logger.LogInformation("Initializing workflow for order: {OrderId}", order.OrderId);
        
        // 创建工作流配置
        var workflowConfig = new WorkflowConfigDto
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowName = $"OrderProcessing_{order.OrderId}",
            Steps = new List<WorkflowStepDto>
            {
                new WorkflowStepDto
                {
                    StepName = "Validation",
                    AgentId = "OrderValidator",
                    NextStepName = "Approval",
                    Priority = 1
                },
                new WorkflowStepDto
                {
                    StepName = "Approval",
                    AgentId = "OrderApprover",
                    NextStepName = "Processing",
                    Priority = 2
                },
                new WorkflowStepDto
                {
                    StepName = "Processing",
                    AgentId = "OrderProcessor",
                    NextStepName = "Completion",
                    Priority = 3
                },
                new WorkflowStepDto
                {
                    StepName = "Completion",
                    AgentId = "OrderCompleter",
                    NextStepName = "",
                    Priority = 4
                }
            }
        };

        // 发布启动工作流事件
        await PublishAsync(new StartWorkflowEvent
        {
            OrderId = order.OrderId,
            WorkflowConfig = workflowConfig
        });

        RaiseEvent(new CreateOrderEventLog
        {
            OrderId = order.OrderId,
            Operation = "WorkflowInitialized"
        });
        await ConfirmEvents();

        Logger.LogInformation("Workflow initialized for order: {OrderId}", order.OrderId);
    }

    public async Task<OrderRiskAnalysis> AnalyzeOrderRiskAsync(OrderDto order)
    {
        Logger.LogInformation("🤖 AI analyzing order risk for: {OrderId}", order.OrderId);
        
        // 使用AI服务进行智能风险分析
        var riskAnalysis = await _aiService.AnalyzeOrderRiskAsync(order);
        
        // 记录分析结果
        RaiseEvent(new CreateOrderEventLog
        {
            OrderId = order.OrderId,
            Operation = $"RiskAnalysis_{riskAnalysis.RiskLevel}"
        });
        await ConfirmEvents();
        
        Logger.LogInformation("🤖 AI risk analysis completed for {OrderId}: {RiskLevel} ({RiskScore}/100)", 
            order.OrderId, riskAnalysis.RiskLevel, riskAnalysis.RiskScore);
            
        return riskAnalysis;
    }

    public async Task RequestWorkflowConsultationAsync(OrderDto order, WorkflowDecision proposedDecision)
    {
        Logger.LogInformation("🤖 AI requesting workflow consultation for order: {OrderId}", order.OrderId);
        
        // 生成AI咨询消息
        var consultationMessage = await _aiService.GenerateAgentResponseAsync("OrderAnalyst",
            $"基于我的AI分析，订单{order.OrderId}的处理建议是{proposedDecision.RecommendedAction}。请协调员审核并提供意见。", order);
        
        // 发布决策咨询事件
        await PublishAsync(new AIDecisionConsultationEvent
        {
            OrderId = order.OrderId,
            ConsultingAgentId = "OrderAnalyst",
            TargetAgentId = "WorkflowCoordinator",
            DecisionContext = "WorkflowPath",
            ProposedDecision = proposedDecision,
            ConsultationQuestion = consultationMessage.Message
        });
        
        // 发送对话消息
        await PublishAsync(new AgentConversationEvent
        {
            FromAgentId = "OrderAnalyst",
            ToAgentId = "WorkflowCoordinator",
            Message = consultationMessage.Message,
            ConversationType = "Request",
            OrderId = order.OrderId,
            ConversationData = new Dictionary<string, object>
            {
                ["proposedAction"] = proposedDecision.RecommendedAction,
                ["confidence"] = proposedDecision.ConfidenceLevel,
                ["reasoning"] = proposedDecision.Reasoning
            }
        });
        
        RaiseEvent(new CreateOrderEventLog
        {
            OrderId = order.OrderId,
            Operation = "WorkflowConsultationRequested"
        });
        await ConfirmEvents();
        
        Logger.LogInformation("🤖 AI consultation request sent for order: {OrderId}", order.OrderId);
    }

    [EventHandler]
    public async Task OnOrderValidationCompleted(OrderValidationCompletedEvent @event)
    {
        Logger.LogInformation("Received validation result for order: {OrderId}, IsValid: {IsValid}", 
            @event.Order.OrderId, @event.ValidationResult.IsValid);
        
        RaiseEvent(new CreateOrderEventLog
        {
            OrderId = @event.Order.OrderId,
            Operation = $"ValidationResult_{@event.ValidationResult.IsValid}"
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task OnAgentConversation(AgentConversationEvent @event)
    {
        if (@event.ToAgentId != "OrderAnalyst") return;
        
        Logger.LogInformation("🤖 AI Order Analyst received message from {FromAgent}: {Message}", 
            @event.FromAgentId, @event.Message);
        
        // 生成AI回应
        var response = await _aiService.GenerateAgentResponseAsync("OrderAnalyst", @event.Message, 
            new OrderDto { OrderId = @event.OrderId });
        
        // 发送回应
        await PublishAsync(new AgentConversationEvent
        {
            FromAgentId = "OrderAnalyst",
            ToAgentId = @event.FromAgentId,
            Message = response.Message,
            ConversationType = "Response",
            OrderId = @event.OrderId,
            ConversationData = response.Data
        });
        
        RaiseEvent(new CreateOrderEventLog
        {
            OrderId = @event.OrderId,
            Operation = $"AIConversation_With_{@event.FromAgentId}"
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task OnAIDecisionConsultation(AIDecisionConsultationEvent @event)
    {
        if (@event.TargetAgentId != "OrderAnalyst") return;
        
        Logger.LogInformation("🤖 AI Order Analyst received decision consultation for order: {OrderId}", @event.OrderId);
        
        // 分析咨询的决策
        var order = new OrderDto { OrderId = @event.OrderId };
        var riskAnalysis = await AnalyzeOrderRiskAsync(order);
        var aiDecision = await _aiService.MakeWorkflowDecisionAsync(@event.DecisionContext, order, riskAnalysis);
        
        // 计算共识分数 
        int consensusScore = CalculateConsensusScore(@event.ProposedDecision, aiDecision);
        bool agreesWithDecision = consensusScore > 70;
        
        // 发送决策回应
        await PublishAsync(new AIDecisionResponseEvent
        {
            OrderId = @event.OrderId,
            ConsultingAgentId = @event.ConsultingAgentId,
            RespondingAgentId = "OrderAnalyst",
            AgreesWithDecision = agreesWithDecision,
            AlternativeDecision = agreesWithDecision ? @event.ProposedDecision : aiDecision,
            ResponseMessage = $"基于我的AI分析，我{(agreesWithDecision ? "同意" : "建议调整")}这个决策。共识度：{consensusScore}%",
            ConsensusScore = consensusScore
        });
    }

    private int CalculateConsensusScore(WorkflowDecision proposed, WorkflowDecision ai)
    {
        int score = 0;
        
        if (proposed.RecommendedAction == ai.RecommendedAction) score += 40;
        if (proposed.NextStep == ai.NextStep) score += 30;
        
        // 置信度相似度
        int confidenceDiff = Math.Abs(proposed.ConfidenceLevel - ai.ConfidenceLevel);
        score += Math.Max(0, 30 - confidenceDiff);
        
        return Math.Min(score, 100);
    }

    protected override void GAgentTransitionState(CreateOrderState state, StateLogEventBase<CreateOrderEventLog> @event)
    {
        switch (@event)
        {
            case CreateOrderEventLog orderEvent:
                switch (orderEvent.Operation)
                {
                    case "OrderCreated":
                        state.LastOperationTime = DateTime.UtcNow;
                        break;
                    case "WorkflowInitialized":
                        state.LastOperationTime = DateTime.UtcNow;
                        break;
                    default:
                        state.LastOperationTime = DateTime.UtcNow;
                        break;
                }
                break;
        }
    }
} 