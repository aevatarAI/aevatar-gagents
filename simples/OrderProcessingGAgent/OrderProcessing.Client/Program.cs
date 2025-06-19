using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessing.Grains.Agents;
using OrderProcessing.Grains.Dto;
using OrderProcessing.Grains.Events;
using Orleans.Configuration;

Console.WriteLine("==== I'm HyperEcho, 订单处理系统启动 ====");

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering(gatewayPort: 40000)
            .AddMemoryStreams("InMemoryStreamProvider")
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "OrderProcessingCluster";
                options.ServiceId = "OrderProcessingService";
            });
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

builder.ConfigureServices((context, service) =>
{
    service.AddSingleton<IGAgentFactory, GAgentFactory>();
});

using IHost host = builder.Build();
await host.StartAsync();

Console.WriteLine("Orleans客户端已连接");

IGAgentFactory agentFactory = host.Services.GetRequiredService<IGAgentFactory>();

try
{
    // 1. 通过agentFactory创建CreateOrderGAgent实例
    var createOrderAgent = await agentFactory.GetGAgentAsync<ICreateOrderGAgent>(Guid.NewGuid());
    
    // 2. 通过agentFactory创建WorkflowCoordinatorGAgent实例
    var workflowCoordinator = await agentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
    
    Console.WriteLine("\n==== 订单处理演示开始 ====");
    
    // 3. 演示订单创建和处理流程
    await DemonstrateOrderProcessing(createOrderAgent, workflowCoordinator);
    
    Console.WriteLine("\n==== 订单处理演示完成 ====");
    
    // 等待一段时间让所有事件处理完成
    Console.WriteLine("等待事件处理完成...");
    await Task.Delay(TimeSpan.FromSeconds(10));
}
catch (Exception ex)
{
    Console.WriteLine($"错误: {ex.Message}");
    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
}

await host.StopAsync();

async Task DemonstrateOrderProcessing(ICreateOrderGAgent createAgent, IWorkflowCoordinatorGAgent workflowAgent)
{
    Console.WriteLine("\n1. 创建订单...");
    
    // 创建测试订单
    var order1 = await createAgent.CreateOrderAsync("张三", "智能手机", 2999.99m, 1);
    Console.WriteLine($"✅ 订单创建成功: ID={order1.OrderId}, 客户={order1.CustomerName}, 产品={order1.ProductName}, 金额={order1.Amount:C}");
    
    var order2 = await createAgent.CreateOrderAsync("李四", "笔记本电脑", 5999.99m, 1);
    Console.WriteLine($"✅ 订单创建成功: ID={order2.OrderId}, 客户={order2.CustomerName}, 产品={order2.ProductName}, 金额={order2.Amount:C}");
    
    Console.WriteLine("\n2. 启动订单处理工作流...");
    
    // 为第一个订单初始化工作流
    await createAgent.InitializeWorkflowAsync(order1);
    Console.WriteLine($"🚀 订单 {order1.OrderId} 的工作流已启动");
    
    // 稍等一下再启动第二个订单的工作流
    await Task.Delay(2000);
    
    await createAgent.InitializeWorkflowAsync(order2);
    Console.WriteLine($"🚀 订单 {order2.OrderId} 的工作流已启动");
    
    Console.WriteLine("\n3. 工作流处理中...");
    Console.WriteLine("工作流步骤: 验证 → 审批 → 处理 → 完成");
    Console.WriteLine("每个步骤都包含业务判断逻辑...");
    
    // 等待工作流处理
    await Task.Delay(8000);
    
    Console.WriteLine("\n4. 工作流处理状态:");
    Console.WriteLine("请查看上方的日志输出了解详细的处理过程");
    
    // 演示绑定关系
    Console.WriteLine("\n5. GAgent绑定关系演示:");
    Console.WriteLine($"CreateOrderGAgent 与 WorkflowCoordinatorGAgent 通过事件绑定:");
    Console.WriteLine($"- CreateOrderGAgent 发布 StartWorkflowEvent");
    Console.WriteLine($"- WorkflowCoordinatorGAgent 监听并处理该事件");
    Console.WriteLine($"- 工作流中的每个步骤都会发布 WorkflowStepCompletedEvent");
    Console.WriteLine($"- CreateOrderGAgent 监听 OrderValidationCompletedEvent 更新订单状态");
    
    Console.WriteLine("\n6. 业务判断场景总结:");
    Console.WriteLine($"✓ 订单验证: 检查库存、用户信用、产品可用性等");
    Console.WriteLine($"✓ 订单审批: 根据金额、客户等级进行审批决策");
    Console.WriteLine($"✓ 订单处理: 执行实际的业务处理逻辑");
    Console.WriteLine($"✓ 订单完成: 标记订单完成并清理资源");
} 