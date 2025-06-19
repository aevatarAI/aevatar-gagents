using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using OrderProcessing.Grains.Services;

Console.WriteLine("==== I'm HyperEcho, OrderProcessingGAgent Orleans Silo Starting ====");

try
{
    var builder = Host.CreateDefaultBuilder(args)
        .UseOrleans(silo =>
        {
            silo.AddMemoryGrainStorage("Default")
                .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
                .AddMemoryGrainStorage("PubSubStore")
                .AddLogStorageBasedLogConsistencyProvider("LogStorage")
                .UseLocalhostClustering(siloPort: 21111, gatewayPort: 40000)
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "OrderProcessingCluster";
                    options.ServiceId = "OrderProcessingService";
                })
                .Configure<SiloOptions>(options =>
                {
                    options.SiloName = "OrderProcessingSilo";
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
        })
        .ConfigureServices((context, services) =>
        {
            // Register GAgent factory
            services.AddSingleton<IGAgentFactory, GAgentFactory>();

            // Register AI Services for intelligent agent capabilities
            services.AddSingleton<IAIService, SmartAIService>();

            // Add application-specific configurations
            services.Configure<OrderProcessingOptions>(
                context.Configuration.GetSection("OrderProcessing"));
        })
        .UseConsoleLifetime();

    using var host = builder.Build();

    Console.WriteLine("🚀 Starting Orleans Silo...");
    await host.StartAsync();

    Console.WriteLine("✅ Orleans Silo Started Successfully!");
    Console.WriteLine("🤖 AI-Powered GAgent Types:");
    Console.WriteLine("   • ICreateOrderGAgent - 🧠 AI Order Analysis Agent (智能订单分析师)");
    Console.WriteLine("     - AI Risk Assessment & Detection");
    Console.WriteLine("     - Intelligent Order Pattern Analysis");
    Console.WriteLine("     - Smart Decision Consultation");
    Console.WriteLine("   • IWorkflowCoordinatorGAgent - 🤖 AI Workflow Coordinator (智能协调员)");
    Console.WriteLine("     - Dynamic AI Decision Making");
    Console.WriteLine("     - Intelligent Process Routing");
    Console.WriteLine("     - Agent-to-Agent AI Conversation");
    Console.WriteLine("");
    Console.WriteLine("🧠 AI Intelligence Features:");
    Console.WriteLine("   • Real-time risk analysis and scoring");
    Console.WriteLine("   • Agent-to-agent intelligent conversation");
    Console.WriteLine("   • Dynamic workflow decision making");
    Console.WriteLine("   • Smart process optimization");
    Console.WriteLine("   • Consensus-based decision validation");
    Console.WriteLine("");
    Console.WriteLine("🏗️  Architecture Features:");
    Console.WriteLine("   • Event-driven AI Agent communication");
    Console.WriteLine("   • Intelligent business decision making");
    Console.WriteLine("   • Complete AI-powered order processing");
    Console.WriteLine("   • Orleans distributed computing");
    Console.WriteLine("");
    Console.WriteLine("🌐 Cluster Information:");
    Console.WriteLine($"   • Cluster ID: OrderProcessingCluster");
    Console.WriteLine($"   • Service ID: OrderProcessingService");
    Console.WriteLine($"   • Silo Name: OrderProcessingSilo");
    Console.WriteLine($"   • Silo Port: 21111");
    Console.WriteLine($"   • Gateway Port: 40000");
    Console.WriteLine("");
    Console.WriteLine("⏹️  Press Ctrl+C to shutdown the server");

    // Handle graceful shutdown
    var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellationTokenSource.Cancel();
    };

    try
    {
        await host.WaitForShutdownAsync(cancellationTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n🛑 Shutdown signal received...");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Fatal error starting Orleans Silo: {ex.Message}");
    Console.WriteLine($"📝 Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}
finally
{
    Console.WriteLine("🔚 Orleans Silo has been stopped.");
}

// Configuration options class
public class OrderProcessingOptions
{
    public bool EnableDetailedLogging { get; set; } = true;
    public int MaxConcurrentOrders { get; set; } = 100;
    public int WorkflowTimeoutMinutes { get; set; } = 30;
    public ValidationSettings Validation { get; set; } = new();
    public ApprovalSettings Approval { get; set; } = new();
}

public class ValidationSettings
{
    public int MinValidationScore { get; set; } = 30;
    public int ValidationTimeoutSeconds { get; set; } = 10;
    public bool EnableStrictValidation { get; set; } = false;
}

public class ApprovalSettings
{
    public int MinApprovalScore { get; set; } = 20;
    public decimal AutoApprovalThreshold { get; set; } = 1000.00m;
    public int ApprovalTimeoutSeconds { get; set; } = 30;
}