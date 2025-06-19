using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrderProcessing.Grains.Dto;

namespace OrderProcessing.Grains.Services;

public class SmartAIService : IAIService
{
    private readonly ILogger<SmartAIService> _logger;

    public SmartAIService(ILogger<SmartAIService> logger)
    {
        _logger = logger;
    }

    public async Task<OrderRiskAnalysis> AnalyzeOrderRiskAsync(OrderDto order)
    {
        _logger.LogInformation("AI analyzing order risk for {OrderId}", order.OrderId);
        
        // 模拟AI风险分析延迟
        await Task.Delay(500);
        
        var riskFactors = new List<string>();
        var recommendations = new List<string>();
        int riskScore = 0;
        
        // AI智能风险评估逻辑
        // 金额风险分析
        if (order.Amount > 10000)
        {
            riskScore += 30;
            riskFactors.Add("高金额订单 (>{amount})".Replace("{amount}", order.Amount.ToString("C")));
            recommendations.Add("建议进行额外的金额验证");
        }
        else if (order.Amount > 5000)
        {
            riskScore += 15;
            riskFactors.Add("中等金额订单");
        }
        
        // 数量风险分析
        if (order.Quantity > 100)
        {
            riskScore += 20;
            riskFactors.Add($"大批量订单 ({order.Quantity}件)");
            recommendations.Add("建议检查库存和供应链能力");
        }
        
        // 客户风险分析（基于客户名称的简单启发式）
        if (order.CustomerName.Contains("test", StringComparison.OrdinalIgnoreCase) ||
            order.CustomerName.Contains("demo", StringComparison.OrdinalIgnoreCase))
        {
            riskScore += 25;
            riskFactors.Add("测试客户订单");
            recommendations.Add("验证是否为真实客户订单");
        }
        
        // 时间风险分析
        var hourOfDay = order.CreateTime.Hour;
        if (hourOfDay < 6 || hourOfDay > 22)
        {
            riskScore += 10;
            riskFactors.Add("非正常时间下单");
        }
        
        // 产品风险分析
        if (order.ProductName.Contains("limited", StringComparison.OrdinalIgnoreCase) ||
            order.ProductName.Contains("exclusive", StringComparison.OrdinalIgnoreCase))
        {
            riskScore += 15;
            riskFactors.Add("限量或独家产品");
            recommendations.Add("确认库存可用性");
        }
        
        // 确定风险等级
        string riskLevel = riskScore switch
        {
            < 20 => "Low",
            < 40 => "Medium", 
            < 70 => "High",
            _ => "Critical"
        };
        
        // 添加通用建议
        if (riskScore > 50)
        {
            recommendations.Add("建议人工审核此订单");
        }
        
        if (riskFactors.Count == 0)
        {
            riskFactors.Add("标准订单，无明显风险因素");
            recommendations.Add("可正常处理");
        }
        
        var analysis = new OrderRiskAnalysis
        {
            RiskScore = Math.Min(riskScore, 100),
            RiskFactors = riskFactors,
            RiskLevel = riskLevel,
            Recommendations = recommendations,
            RequiresHumanReview = riskScore > 60
        };
        
        _logger.LogInformation("AI risk analysis completed: Score={RiskScore}, Level={RiskLevel}", 
            analysis.RiskScore, analysis.RiskLevel);
            
        return analysis;
    }

    public async Task<WorkflowDecision> MakeWorkflowDecisionAsync(string stepName, OrderDto order, OrderRiskAnalysis riskAnalysis)
    {
        _logger.LogInformation("AI making workflow decision for step {StepName}, order {OrderId}", stepName, order.OrderId);
        
        await Task.Delay(300);
        
        var decision = new WorkflowDecision();
        
        switch (stepName)
        {
            case "Validation":
                if (riskAnalysis.RiskScore < 30)
                {
                    decision.RecommendedAction = "Approve";
                    decision.NextStep = "Approval";
                    decision.ConfidenceLevel = 95;
                    decision.Reasoning = "低风险订单，建议快速通过验证";
                }
                else if (riskAnalysis.RiskScore < 60)
                {
                    decision.RecommendedAction = "Review";
                    decision.NextStep = "Approval";
                    decision.ConfidenceLevel = 80;
                    decision.Reasoning = "中等风险订单，建议审慎验证后通过";
                }
                else
                {
                    decision.RecommendedAction = "Escalate";
                    decision.NextStep = "ManualReview";
                    decision.ConfidenceLevel = 90;
                    decision.Reasoning = "高风险订单，建议升级至人工审核";
                }
                break;
                
            case "Approval":
                if (riskAnalysis.RequiresHumanReview)
                {
                    decision.RecommendedAction = "Escalate";
                    decision.NextStep = "ManualApproval";
                    decision.ConfidenceLevel = 95;
                    decision.Reasoning = "根据风险分析，需要人工审批";
                }
                else if (order.Amount > 5000)
                {
                    decision.RecommendedAction = "Review";
                    decision.NextStep = "Processing";
                    decision.ConfidenceLevel = 85;
                    decision.Reasoning = "大金额订单，审慎批准";
                }
                else
                {
                    decision.RecommendedAction = "Approve";
                    decision.NextStep = "Processing";
                    decision.ConfidenceLevel = 90;
                    decision.Reasoning = "标准订单，建议批准";
                }
                break;
                
            case "Processing":
                decision.RecommendedAction = "Approve";
                decision.NextStep = "Completion";
                decision.ConfidenceLevel = 95;
                decision.Reasoning = "进入处理阶段，预计正常完成";
                break;
                
            default:
                decision.RecommendedAction = "Review";
                decision.NextStep = "ManualReview";
                decision.ConfidenceLevel = 50;
                decision.Reasoning = $"未知步骤 {stepName}，建议人工介入";
                break;
        }
        
        decision.AdditionalData["originalStep"] = stepName;
        decision.AdditionalData["riskScore"] = riskAnalysis.RiskScore;
        decision.AdditionalData["orderAmount"] = order.Amount;
        
        _logger.LogInformation("AI decision: {Action} with {Confidence}% confidence", 
            decision.RecommendedAction, decision.ConfidenceLevel);
            
        return decision;
    }

    public async Task<AgentResponse> GenerateAgentResponseAsync(string agentRole, string message, OrderDto order)
    {
        _logger.LogInformation("AI generating response for agent role {AgentRole}", agentRole);
        
        await Task.Delay(200);
        
        var response = new AgentResponse();
        
        switch (agentRole.ToLower())
        {
            case "orderanalyst":
                response.Message = GenerateAnalystResponse(message, order);
                response.Intent = "analysis";
                break;
                
            case "workflowcoordinator":
                response.Message = GenerateCoordinatorResponse(message, order);
                response.Intent = "coordination";
                break;
                
            default:
                response.Message = $"作为{agentRole}，我收到了消息：{message}。正在分析订单{order.OrderId}的情况。";
                response.Intent = "general";
                break;
        }
        
        response.Data["agentRole"] = agentRole;
        response.Data["orderId"] = order.OrderId;
        response.Data["timestamp"] = DateTime.UtcNow;
        response.RequiresResponse = message.Contains("?") || message.Contains("请") || message.Contains("建议");
        
        return response;
    }

    private string GenerateAnalystResponse(string message, OrderDto order)
    {
        var responses = new[]
        {
            $"我已完成订单{order.OrderId}的深度分析。客户{order.CustomerName}的订单金额{order.Amount:C}，涉及{order.ProductName}产品。基于我的AI分析，这个订单呈现出特定的风险特征。",
            $"根据我的智能分析，订单{order.OrderId}需要特别关注。金额{order.Amount:C}和数量{order.Quantity}的组合显示了某些值得注意的模式。",
            $"我的AI算法检测到订单{order.OrderId}具有独特的特征组合。建议工作流协调员考虑这些因素进行路径决策。",
            $"分析完成！订单{order.OrderId}的客户行为模式和订单特征已被我的AI系统全面评估。有什么具体方面需要深入讨论吗？"
        };
        
        return responses[new Random().Next(responses.Length)];
    }

    private string GenerateCoordinatorResponse(string message, OrderDto order)
    {
        var responses = new[]
        {
            $"收到分析师的见解。基于AI决策引擎，我将为订单{order.OrderId}制定最优处理路径。当前状态为{order.Status}，我正在评估最佳的下一步行动。",
            $"明白！我的AI协调算法正在处理订单{order.OrderId}。考虑到分析结果，我将智能调度工作流步骤以确保最高效率。",
            $"作为智能协调员，我已接收到关于订单{order.OrderId}的关键信息。我的AI系统正在生成个性化的处理策略。",
            $"协调决策中...基于多维度分析，订单{order.OrderId}将被路由到最适合的处理分支。我的AI算法确保了最优的资源分配。"
        };
        
        return responses[new Random().Next(responses.Length)];
    }

    public async Task<List<string>> GenerateSmartSuggestionsAsync(OrderDto order, string context)
    {
        _logger.LogInformation("AI generating smart suggestions for order {OrderId} in context {Context}", 
            order.OrderId, context);
            
        await Task.Delay(300);
        
        var suggestions = new List<string>();
        
        switch (context.ToLower())
        {
            case "validation":
                suggestions.AddRange(new[]
                {
                    $"验证客户{order.CustomerName}的历史订单模式",
                    $"检查{order.ProductName}的当前库存状态",
                    $"确认{order.Amount:C}金额的支付方式可靠性",
                    "运行实时反欺诈检测算法"
                });
                break;
                
            case "approval":
                suggestions.AddRange(new[]
                {
                    "基于客户信用评级进行智能风险评估",
                    $"考虑{order.Quantity}件商品的供应链影响",
                    "启动多层级智能审批工作流",
                    "集成外部数据源进行全面评估"
                });
                break;
                
            case "processing":
                suggestions.AddRange(new[]
                {
                    "优化仓储和物流路径选择",
                    "预测交付时间并主动通知客户",
                    "监控处理进度并提供实时更新",
                    "准备智能化的异常处理预案"
                });
                break;
                
            default:
                suggestions.AddRange(new[]
                {
                    "运行全方位AI健康检查",
                    "优化整体工作流效率",
                    "预测并预防潜在问题",
                    "收集反馈以持续改进AI模型"
                });
                break;
        }
        
        return suggestions;
    }
} 