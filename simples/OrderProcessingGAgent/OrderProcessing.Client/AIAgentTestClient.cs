using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessing.Grains.Agents;
using OrderProcessing.Grains.Dto;
using OrderProcessing.Grains.Events;
using OrderProcessing.Grains.Services;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace OrderProcessing.Client;

/// <summary>
/// AI Agent 测试客户端 - 演示两个AI Agent的智能协作流处理
/// </summary>
public class AIAgentTestClient
{
    private readonly IClusterClient _client;
    private readonly ILogger<AIAgentTestClient> _logger;

    public AIAgentTestClient(IClusterClient client, ILogger<AIAgentTestClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// 演示AI Agent智能协作流处理
    /// </summary>
    public async Task RunAIAgentDemoAsync()
    {
        Console.WriteLine("🤖=== AI Agent 智能协作流处理演示 ===");
        Console.WriteLine();

        try
        {
            // 测试场景1: 低风险订单 - AI快速处理
            await TestLowRiskOrderScenario();
            
            await Task.Delay(2000);
            
            // 测试场景2: 高风险订单 - AI智能决策
            await TestHighRiskOrderScenario();
            
            await Task.Delay(2000);
            
            // 测试场景3: 复杂场景 - Agent对话协商
            await TestAgentConversationScenario();
            
            Console.WriteLine("\n🎉 AI Agent 智能流处理演示完成!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Agent demo failed");
            Console.WriteLine($"❌ Demo失败: {ex.Message}");
        }
    }

    private async Task TestLowRiskOrderScenario()
    {
        Console.WriteLine("🧪 场景1: 低风险订单 - AI智能快速处理");
        Console.WriteLine(new string('-', 50));

        var orderAnalyst = _client.GetGrain<ICreateOrderGAgent>(Guid.NewGuid());
        var workflowCoordinator = _client.GetGrain<IWorkflowCoordinatorGAgent>(Guid.NewGuid());

        Console.WriteLine("📝 创建标准订单...");
        var order = await orderAnalyst.CreateOrderAsync(
            customerName: "Alice Johnson",
            productName: "Laptop Computer",
            amount: 1299.99m,
            quantity: 1
        );

        Console.WriteLine($"✅ 订单创建: {order.OrderId}");
        Console.WriteLine($"   客户: {order.CustomerName}");
        Console.WriteLine($"   产品: {order.ProductName}");
        Console.WriteLine($"   金额: {order.Amount:C}");
        Console.WriteLine($"   数量: {order.Quantity}");

        // AI分析师进行风险分析
        Console.WriteLine("\n🤖 AI分析师正在进行风险评估...");
        var riskAnalysis = await orderAnalyst.AnalyzeOrderRiskAsync(order);
        
        Console.WriteLine($"📊 AI风险分析结果:");
        Console.WriteLine($"   风险评分: {riskAnalysis.RiskScore}/100");
        Console.WriteLine($"   风险等级: {riskAnalysis.RiskLevel}");
        Console.WriteLine($"   风险因素: {string.Join(", ", riskAnalysis.RiskFactors)}");
        Console.WriteLine($"   AI建议: {string.Join("; ", riskAnalysis.Recommendations)}");

        // 启动AI工作流
        Console.WriteLine("\n🚀 启动AI智能工作流...");
        await orderAnalyst.InitializeWorkflowAsync(order);

        await Task.Delay(3000); // 等待AI处理
        Console.WriteLine("✅ 低风险订单AI处理完成\n");
    }

    private async Task TestHighRiskOrderScenario()
    {
        Console.WriteLine("🧪 场景2: 高风险订单 - AI智能风控决策");
        Console.WriteLine(new string('-', 50));

        var orderAnalyst = _client.GetGrain<ICreateOrderGAgent>(Guid.NewGuid());
        var workflowCoordinator = _client.GetGrain<IWorkflowCoordinatorGAgent>(Guid.NewGuid());

        Console.WriteLine("⚠️ 创建高风险订单...");
        var highRiskOrder = await orderAnalyst.CreateOrderAsync(
            customerName: "Test_User_Demo",  // 触发测试客户风险
            productName: "Limited Edition Luxury Watch",  // 限量产品
            amount: 15999.99m,  // 高金额
            quantity: 150  // 大批量
        );

        Console.WriteLine($"⚠️ 高风险订单创建: {highRiskOrder.OrderId}");
        Console.WriteLine($"   客户: {highRiskOrder.CustomerName}");
        Console.WriteLine($"   产品: {highRiskOrder.ProductName}"); 
        Console.WriteLine($"   金额: {highRiskOrder.Amount:C}");
        Console.WriteLine($"   数量: {highRiskOrder.Quantity}");

        // AI深度风险分析
        Console.WriteLine("\n🤖 AI深度风险分析中...");
        var highRiskAnalysis = await orderAnalyst.AnalyzeOrderRiskAsync(highRiskOrder);
        
        Console.WriteLine($"🚨 AI高风险分析结果:");
        Console.WriteLine($"   风险评分: {highRiskAnalysis.RiskScore}/100");
        Console.WriteLine($"   风险等级: {highRiskAnalysis.RiskLevel}");
        Console.WriteLine($"   需要人工审核: {(highRiskAnalysis.RequiresHumanReview ? "是" : "否")}");
        Console.WriteLine($"   风险因素:");
        foreach (var factor in highRiskAnalysis.RiskFactors)
            Console.WriteLine($"     • {factor}");
        Console.WriteLine($"   AI建议:");
        foreach (var rec in highRiskAnalysis.Recommendations)
            Console.WriteLine($"     • {rec}");

        // 启动高风险订单AI工作流
        Console.WriteLine("\n🤖 启动高风险AI智能处理流程...");
        await orderAnalyst.InitializeWorkflowAsync(highRiskOrder);

        await Task.Delay(4000); // 等待AI复杂处理
        Console.WriteLine("✅ 高风险订单AI智能决策完成\n");
    }

    private async Task TestAgentConversationScenario()
    {
        Console.WriteLine("🧪 场景3: Agent智能对话协商场景");
        Console.WriteLine(new string('-', 50));

        var orderAnalyst = _client.GetGrain<ICreateOrderGAgent>(Guid.NewGuid());
        var workflowCoordinator = _client.GetGrain<IWorkflowCoordinatorGAgent>(Guid.NewGuid());

        Console.WriteLine("💬 创建需要协商的复杂订单...");
        var complexOrder = await orderAnalyst.CreateOrderAsync(
            customerName: "VIP Customer Corp",
            productName: "Enterprise Software License", 
            amount: 8500.00m,
            quantity: 50
        );

        Console.WriteLine($"💼 复杂订单: {complexOrder.OrderId}");
        Console.WriteLine($"   类型: 企业级订单");
        Console.WriteLine($"   需要: AI Agent协商决策");

        // AI分析师分析
        Console.WriteLine("\n🤖 AI分析师深度分析...");
        var analysis = await orderAnalyst.AnalyzeOrderRiskAsync(complexOrder);
        
        // 模拟AI决策咨询场景
        Console.WriteLine("\n💭 AI分析师向协调员发起决策咨询...");
        
        var proposedDecision = new WorkflowDecision
        {
            RecommendedAction = "Review",
            NextStep = "Approval",
            ConfidenceLevel = 75,
            Reasoning = "中等风险企业订单，建议审慎处理"
        };

        await orderAnalyst.RequestWorkflowConsultationAsync(complexOrder, proposedDecision);

        Console.WriteLine("🤝 AI Agent智能对话协商进行中...");
        Console.WriteLine("   • 分析师: 基于风险评估建议审慎处理");
        Console.WriteLine("   • 协调员: 评估处理能力和资源分配");
        Console.WriteLine("   • 共识: 达成最优处理方案");

        // 启动协商后的工作流
        await orderAnalyst.InitializeWorkflowAsync(complexOrder);

        await Task.Delay(3000);
        Console.WriteLine("✅ AI Agent智能协商完成，达成处理共识\n");
    }

    /// <summary>
    /// 演示AI Agent状态监控
    /// </summary>
    public async Task ShowAIAgentStatus()
    {
        Console.WriteLine("📊 AI Agent 状态监控");
        Console.WriteLine(new string('=', 40));

        var analyst = _client.GetGrain<ICreateOrderGAgent>(Guid.NewGuid());
        var coordinator = _client.GetGrain<IWorkflowCoordinatorGAgent>(Guid.NewGuid());

        var analystDesc = await analyst.GetDescriptionAsync();
        var coordinatorDesc = await coordinator.GetDescriptionAsync();

        Console.WriteLine($"🧠 AI订单分析师: {analystDesc}");
        Console.WriteLine($"🤖 AI工作流协调员: {coordinatorDesc}");
        Console.WriteLine();
    }
}

/// <summary>
/// AI Agent测试程序入口
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🤖 AI Agent 智能协作流处理测试客户端");
        Console.WriteLine("============================================");
        Console.WriteLine();

        var client = await CreateOrleansClientAsync();
        var logger = CreateLogger();

        try
        {
            var testClient = new AIAgentTestClient(client, logger);
            
            await testClient.ShowAIAgentStatus();
            await testClient.RunAIAgentDemoAsync();

            Console.WriteLine("\n🎯 演示总结:");
            Console.WriteLine("✓ AI智能风险评估");
            Console.WriteLine("✓ 动态工作流决策");
            Console.WriteLine("✓ Agent间智能对话");
            Console.WriteLine("✓ 共识决策机制");
            Console.WriteLine("✓ 自适应流程优化");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败: {ex.Message}");
        }
        finally
        {
            if (client != null && client is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    private static async Task<IClusterClient> CreateOrleansClientAsync()
    {
        var services = new ServiceCollection();
        var client = services.AddOrleansClient(builder =>
        {
            builder.UseLocalhostClustering(gatewayPort: 40000)
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "OrderProcessingCluster";
                    options.ServiceId = "OrderProcessingService";
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
        }).BuildServiceProvider().GetRequiredService<IClusterClient>();

        Console.WriteLine("🔗 连接到Orleans集群...");
        await client.Connect();
        Console.WriteLine("✅ 连接成功!");
        Console.WriteLine();

        return client;
    }

    private static ILogger<AIAgentTestClient> CreateLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        return loggerFactory.CreateLogger<AIAgentTestClient>();
    }
} 