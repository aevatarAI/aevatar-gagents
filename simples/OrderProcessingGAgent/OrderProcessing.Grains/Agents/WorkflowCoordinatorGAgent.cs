using System;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using OrderProcessing.Grains.Dto;
using OrderProcessing.Grains.Events;
using OrderProcessing.Grains.Services;

namespace OrderProcessing.Grains.Agents;

public interface IWorkflowCoordinatorGAgent : IGAgent
{
    Task StartWorkflowAsync(WorkflowConfigDto config, Guid orderId);
    Task ExecuteStepAsync(string stepName, Guid orderId);
    Task CompleteStepAsync(string stepName, Guid orderId, bool success, string message);
    Task MakeIntelligentDecisionAsync(string stepName, Guid orderId, OrderDto order);
    Task HandleAgentConsultationAsync(AIDecisionConsultationEvent consultationEvent);
}

[GenerateSerializer]
public class WorkflowState : StateBase
{
    [Id(0)]
    public Dictionary<Guid, WorkflowConfigDto> ActiveWorkflows { get; set; } = new();
    
    [Id(1)]
    public Dictionary<Guid, string> CurrentSteps { get; set; } = new();
    
    [Id(2)]
    public Dictionary<Guid, List<string>> CompletedSteps { get; set; } = new();
    
    [Id(3)]
    public Dictionary<Guid, OrderStatus> OrderStatuses { get; set; } = new();
}

[GenerateSerializer]
public class WorkflowEventLog : StateLogEventBase<WorkflowEventLog>
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public string StepName { get; set; } = string.Empty;
    
    [Id(2)]
    public string Action { get; set; } = string.Empty;
}

[GAgent(nameof(WorkflowCoordinatorGAgent))]
public class WorkflowCoordinatorGAgent : GAgentBase<WorkflowState, WorkflowEventLog>, IWorkflowCoordinatorGAgent
{
    private readonly IAIService _aiService;

    public WorkflowCoordinatorGAgent(IAIService aiService)
    {
        _aiService = aiService;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("🤖 AI-Powered Workflow Coordinator - 智能工作流协调员，专注于动态决策、智能路由和流程优化");
    }

    public async Task StartWorkflowAsync(WorkflowConfigDto config, Guid orderId)
    {
        Logger.LogInformation("Starting workflow for order: {OrderId}", orderId);
        
        // 找到第一个步骤（Priority = 1）
        var firstStep = config.Steps.OrderBy(s => s.Priority).FirstOrDefault();
        if (firstStep != null)
        {
            RaiseEvent(new WorkflowEventLog
            {
                OrderId = orderId,
                StepName = firstStep.StepName,
                Action = "WorkflowStarted"
            });
            await ConfirmEvents();

            // 执行第一个步骤
            await ExecuteStepAsync(firstStep.StepName, orderId);
        }
        
        Logger.LogInformation("Workflow started for order: {OrderId}", orderId);
    }

    public async Task ExecuteStepAsync(string stepName, Guid orderId)
    {
        Logger.LogInformation("Executing step: {StepName} for order: {OrderId}", stepName, orderId);
        
        if (!State.ActiveWorkflows.ContainsKey(orderId))
        {
            Logger.LogWarning("No active workflow found for order: {OrderId}", orderId);
            return;
        }

        var workflow = State.ActiveWorkflows[orderId];
        var step = workflow.Steps.FirstOrDefault(s => s.StepName == stepName);
        
        if (step == null)
        {
            Logger.LogWarning("Step not found: {StepName} for order: {OrderId}", stepName, orderId);
            return;
        }

        RaiseEvent(new WorkflowEventLog
        {
            OrderId = orderId,
            StepName = stepName,
            Action = "StepExecuting"
        });
        await ConfirmEvents();

        // 根据步骤类型执行不同的业务逻辑
        await ExecuteStepLogic(stepName, orderId, step);
    }

    private async Task ExecuteStepLogic(string stepName, Guid orderId, WorkflowStepDto step)
    {
        Logger.LogInformation("🤖 AI Coordinator: Executing intelligent step logic: {StepName} for order: {OrderId}", stepName, orderId);
        
        // 创建订单DTO用于AI决策
        var order = new OrderDto { OrderId = orderId, Status = GetCurrentOrderStatus(orderId) };
        
        // 使用AI进行智能决策
        await MakeIntelligentDecisionAsync(stepName, orderId, order);
    }

    public async Task MakeIntelligentDecisionAsync(string stepName, Guid orderId, OrderDto order)
    {
        Logger.LogInformation("🤖 AI making intelligent decision for step {StepName}, order {OrderId}", stepName, orderId);
        
        // 获取AI风险分析
        var riskAnalysis = await _aiService.AnalyzeOrderRiskAsync(order);
        
        // 获取AI工作流决策
        var aiDecision = await _aiService.MakeWorkflowDecisionAsync(stepName, order, riskAnalysis);
        
        // 生成智能建议
        var suggestions = await _aiService.GenerateSmartSuggestionsAsync(order, stepName.ToLower());
        
        // 发送AI对话消息给分析师
        var aiResponse = await _aiService.GenerateAgentResponseAsync("WorkflowCoordinator",
            $"正在处理订单{orderId}的{stepName}步骤，AI决策建议：{aiDecision.RecommendedAction}，置信度{aiDecision.ConfidenceLevel}%", order);
        
        await PublishAsync(new AgentConversationEvent
        {
            FromAgentId = "WorkflowCoordinator",
            ToAgentId = "OrderAnalyst",
            Message = aiResponse.Message,
            ConversationType = "Notification",
            OrderId = orderId,
            ConversationData = new Dictionary<string, object>
            {
                ["stepName"] = stepName,
                ["aiDecision"] = aiDecision.RecommendedAction,
                ["confidence"] = aiDecision.ConfidenceLevel,
                ["suggestions"] = suggestions
            }
        });
        
        // 根据AI决策执行相应逻辑
        bool stepResult = await ExecuteAIDecision(stepName, orderId, aiDecision, riskAnalysis);
        
        // 发布智能工作流调整事件
        await PublishAsync(new SmartWorkflowAdjustmentEvent
        {
            OrderId = orderId,
            AdjustingAgentId = "WorkflowCoordinator", 
            OriginalStep = stepName,
            NewStep = aiDecision.NextStep,
            AdjustmentReason = aiDecision.Reasoning,
            AIDecision = aiDecision
        });
        
        await CompleteStepAsync(stepName, orderId, stepResult, aiDecision.Reasoning);
    }

    private async Task<bool> ExecuteAIDecision(string stepName, Guid orderId, WorkflowDecision aiDecision, OrderRiskAnalysis riskAnalysis)
    {
        switch (aiDecision.RecommendedAction)
        {
            case "Approve":
                Logger.LogInformation("🤖 AI Decision: Approving {StepName} for order {OrderId}", stepName, orderId);
                await Task.Delay(200); // 模拟快速处理
                return true;
                
            case "Review":
                Logger.LogInformation("🤖 AI Decision: Reviewing {StepName} for order {OrderId}", stepName, orderId);
                await Task.Delay(1000); // 模拟审慎处理
                return riskAnalysis.RiskScore < 70; // 基于风险评分决定
                
            case "Reject":
                Logger.LogInformation("🤖 AI Decision: Rejecting {StepName} for order {OrderId}", stepName, orderId);
                return false;
                
            case "Escalate":
                Logger.LogInformation("🤖 AI Decision: Escalating {StepName} for order {OrderId} to human review", stepName, orderId);
                await Task.Delay(500);
                // 在实际场景中，这里会触发人工审核流程
                return riskAnalysis.RiskScore < 80; // 临时处理，实际应等待人工决策
                
            default:
                Logger.LogWarning("🤖 Unknown AI decision action: {Action}", aiDecision.RecommendedAction);
                return false;
        }
    }

    public async Task HandleAgentConsultationAsync(AIDecisionConsultationEvent consultationEvent)
    {
        Logger.LogInformation("🤖 AI Coordinator handling consultation from {ConsultingAgent} for order {OrderId}", 
            consultationEvent.ConsultingAgentId, consultationEvent.OrderId);
        
        // 获取订单信息
        var order = new OrderDto { OrderId = consultationEvent.OrderId };
        var riskAnalysis = await _aiService.AnalyzeOrderRiskAsync(order);
        
        // 生成AI回应
        var coordinatorDecision = await _aiService.MakeWorkflowDecisionAsync(consultationEvent.DecisionContext, order, riskAnalysis);
        
        // 计算与提议决策的一致性
        int consensusScore = CalculateDecisionConsensus(consultationEvent.ProposedDecision, coordinatorDecision);
        bool agrees = consensusScore > 75;
        
        // 生成回应消息
        var responseMessage = await _aiService.GenerateAgentResponseAsync("WorkflowCoordinator",
            consultationEvent.ConsultationQuestion, order);
        
        // 发送决策回应
        await PublishAsync(new AIDecisionResponseEvent
        {
            OrderId = consultationEvent.OrderId,
            ConsultingAgentId = consultationEvent.ConsultingAgentId,
            RespondingAgentId = "WorkflowCoordinator",
            AgreesWithDecision = agrees,
            AlternativeDecision = agrees ? consultationEvent.ProposedDecision : coordinatorDecision,
            ResponseMessage = $"{responseMessage.Message} 共识度：{consensusScore}%",
            ConsensusScore = consensusScore
        });
        
        // 记录咨询处理
        RaiseEvent(new WorkflowEventLog
        {
            OrderId = consultationEvent.OrderId,
            StepName = consultationEvent.DecisionContext,
            Action = $"ConsultationHandled_Consensus{consensusScore}"
        });
        await ConfirmEvents();
    }

    private int CalculateDecisionConsensus(WorkflowDecision proposed, WorkflowDecision coordinator)
    {
        int score = 0;
        if (proposed.RecommendedAction == coordinator.RecommendedAction) score += 50;
        if (proposed.NextStep == coordinator.NextStep) score += 30;
        
        int confidenceDiff = Math.Abs(proposed.ConfidenceLevel - coordinator.ConfidenceLevel);
        score += Math.Max(0, 20 - confidenceDiff / 5);
        
        return Math.Min(score, 100);
    }

    private OrderStatus GetCurrentOrderStatus(Guid orderId)
    {
        if (State.OrderStatuses.TryGetValue(orderId, out var status))
            return status;
        return OrderStatus.Created;
    }

    public async Task CompleteStepAsync(string stepName, Guid orderId, bool success, string message)
    {
        Logger.LogInformation("Completing step: {StepName} for order: {OrderId}, Success: {Success}", 
            stepName, orderId, success);
        
        if (!State.ActiveWorkflows.ContainsKey(orderId))
        {
            Logger.LogWarning("No active workflow found for order: {OrderId}", orderId);
            return;
        }

        var workflow = State.ActiveWorkflows[orderId];
        var step = workflow.Steps.FirstOrDefault(s => s.StepName == stepName);
        
        if (step == null)
        {
            Logger.LogWarning("Step not found: {StepName}", stepName);
            return;
        }

        RaiseEvent(new WorkflowEventLog
        {
            OrderId = orderId,
            StepName = stepName,
            Action = success ? "StepCompleted" : "StepFailed"
        });
        await ConfirmEvents();

        // 发布步骤完成事件
        await PublishAsync(new WorkflowStepCompletedEvent
        {
            OrderId = orderId,
            CompletedStep = stepName,
            NextStep = step.NextStepName,
            IsSuccess = success,
            Message = message
        });

        // 如果成功且有下一步，继续执行
        if (success && !string.IsNullOrEmpty(step.NextStepName))
        {
            Logger.LogInformation("Moving to next step: {NextStep}", step.NextStepName);
            await ExecuteStepAsync(step.NextStepName, orderId);
        }
        else if (success && string.IsNullOrEmpty(step.NextStepName))
        {
            // 工作流完成
            await CompleteWorkflow(orderId, true);
        }
        else
        {
            // 步骤失败，工作流失败
            await CompleteWorkflow(orderId, false);
        }
    }

    private async Task CompleteWorkflow(Guid orderId, bool success)
    {
        Logger.LogInformation("Completing workflow for order: {OrderId}, Success: {Success}", orderId, success);
        
        RaiseEvent(new WorkflowEventLog
        {
            OrderId = orderId,
            StepName = "Workflow",
            Action = success ? "WorkflowCompleted" : "WorkflowFailed"
        });
        await ConfirmEvents();

        // 发布工作流完成事件
        await PublishAsync(new WorkflowCompletedEvent
        {
            OrderId = orderId,
            FinalStatus = success ? OrderStatus.Completed : OrderStatus.Rejected,
            Message = success ? "Order processing completed successfully" : "Order processing failed"
        });
    }

    private async Task<bool> ValidateOrder(Guid orderId)
    {
        // 模拟业务验证逻辑 - 简单的随机结果，基于订单ID
        await Task.Delay(100);
        var score = Math.Abs(orderId.GetHashCode()) % 100;
        return score > 30; // 70% 通过率
    }

    private async Task<bool> ApproveOrder(Guid orderId)
    {
        // 模拟审批逻辑
        await Task.Delay(100);
        var score = Math.Abs(orderId.GetHashCode()) % 50;
        return score > 20; // 60% 通过率
    }

    [EventHandler]
    public async Task OnStartWorkflow(StartWorkflowEvent @event)
    {
        Logger.LogInformation("🤖 AI Coordinator received StartWorkflow event for order: {OrderId}", @event.OrderId);
        await StartWorkflowAsync(@event.WorkflowConfig, @event.OrderId);
    }

    [EventHandler]
    public async Task OnAgentConversation(AgentConversationEvent @event)
    {
        if (@event.ToAgentId != "WorkflowCoordinator") return;
        
        Logger.LogInformation("🤖 AI Workflow Coordinator received message from {FromAgent}: {Message}", 
            @event.FromAgentId, @event.Message);
        
        // 生成AI回应
        var response = await _aiService.GenerateAgentResponseAsync("WorkflowCoordinator", @event.Message, 
            new OrderDto { OrderId = @event.OrderId });
        
        // 发送回应
        await PublishAsync(new AgentConversationEvent
        {
            FromAgentId = "WorkflowCoordinator",
            ToAgentId = @event.FromAgentId,
            Message = response.Message,
            ConversationType = "Response",
            OrderId = @event.OrderId,
            ConversationData = response.Data
        });
        
        RaiseEvent(new WorkflowEventLog
        {
            OrderId = @event.OrderId,
            StepName = "AIConversation",
            Action = $"RespondedTo_{@event.FromAgentId}"
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task OnAIDecisionConsultation(AIDecisionConsultationEvent @event)
    {
        if (@event.TargetAgentId != "WorkflowCoordinator") return;
        
        Logger.LogInformation("🤖 AI Workflow Coordinator received consultation for order: {OrderId}", @event.OrderId);
        await HandleAgentConsultationAsync(@event);
    }

    [EventHandler]
    public async Task OnAIAnalysisResponse(AIAnalysisResponseEvent @event)
    {
        Logger.LogInformation("🤖 AI Coordinator received analysis from {RespondingAgent} for order {OrderId}: Risk {RiskLevel}", 
            @event.RespondingAgentId, @event.OrderId, @event.RiskAnalysis.RiskLevel);
        
        // 基于AI分析结果调整工作流策略
        if (@event.RiskAnalysis.RequiresHumanReview)
        {
            Logger.LogInformation("🤖 High risk detected, adjusting workflow for human review");
            
            await PublishAsync(new SmartWorkflowAdjustmentEvent
            {
                OrderId = @event.OrderId,
                AdjustingAgentId = "WorkflowCoordinator",
                OriginalStep = "AutomatedProcessing",
                NewStep = "HumanReview", 
                AdjustmentReason = "High risk analysis requires human intervention",
                AIDecision = new WorkflowDecision 
                { 
                    RecommendedAction = "Escalate",
                    NextStep = "HumanReview",
                    Reasoning = @event.AnalysisMessage
                }
            });
        }
        
        RaiseEvent(new WorkflowEventLog
        {
            OrderId = @event.OrderId,
            StepName = "AIAnalysisProcessed",
            Action = $"RiskLevel_{@event.RiskAnalysis.RiskLevel}"
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(WorkflowState state, StateLogEventBase<WorkflowEventLog> @event)
    {
        switch (@event)
        {
            case WorkflowEventLog workflowEvent:
                switch (workflowEvent.Action)
                {
                    case "WorkflowStarted":
                        if (!state.CompletedSteps.ContainsKey(workflowEvent.OrderId))
                        {
                            state.CompletedSteps[workflowEvent.OrderId] = new List<string>();
                        }
                        state.CurrentSteps[workflowEvent.OrderId] = workflowEvent.StepName;
                        state.OrderStatuses[workflowEvent.OrderId] = OrderStatus.Processing;
                        break;
                    case "StepCompleted":
                        if (state.CompletedSteps.ContainsKey(workflowEvent.OrderId))
                        {
                            state.CompletedSteps[workflowEvent.OrderId].Add(workflowEvent.StepName);
                        }
                        break;
                    case "WorkflowCompleted":
                        state.OrderStatuses[workflowEvent.OrderId] = OrderStatus.Completed;
                        if (state.CurrentSteps.ContainsKey(workflowEvent.OrderId))
                        {
                            state.CurrentSteps.Remove(workflowEvent.OrderId);
                        }
                        break;
                    case "WorkflowFailed":
                        state.OrderStatuses[workflowEvent.OrderId] = OrderStatus.Rejected;
                        if (state.CurrentSteps.ContainsKey(workflowEvent.OrderId))
                        {
                            state.CurrentSteps.Remove(workflowEvent.OrderId);
                        }
                        break;
                }
                break;
        }
    }
} 