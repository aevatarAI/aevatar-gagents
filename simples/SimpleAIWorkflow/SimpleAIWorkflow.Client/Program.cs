using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.Basic.PublishGAgent;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.Station.Feature.CreatorGAgent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost;
using Orleans.Hosting;
using SimpleAIWorkflow.Client;
using SimpleAIWorkflow.Grains;

Console.WriteLine("🚀 SimpleAIWorkflow - Intelligent AI Workflow System Starting...");
Console.WriteLine("===============================================");

// Use TestingHost for self-contained execution
var builder = new TestClusterBuilder();
builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
builder.AddClientBuilderConfigurator<TestClientConfigurator>();

var cluster = builder.Build();
await cluster.DeployAsync();

var client = cluster.Client;
var agentFactory = client.ServiceProvider.GetService<IGAgentFactory>();

Console.WriteLine("🔧 System initialization completed, creating intelligent workflow agents...");

try
{
    // Create CreatorService
    var creatorService = new CreatorService(agentFactory);
    
    Console.WriteLine("📋 Creating specialized AI workflow agents:");
    
    // 1. Create data validation agent
    var validatorId = Guid.NewGuid();
    await creatorService.CreateAgentAsync(new CreateAgentInputDto
    {
        AgentId = validatorId,
        AgentType = nameof(WorkflowAIAgent),
        Name = "Data Validator",
        Properties = new Dictionary<string, object>
        {
            { "WorkflowType", "validation" },
            { "TaskDescription", "Responsible for data validation and format checking" },
            { "Priority", 1 },
            { "TimeoutMilliseconds", 30000 }
        }
    });
    Console.WriteLine("   ✅ Data Validator created");
    
    // 2. Create risk analysis agent
    var analystId = Guid.NewGuid();
    await creatorService.CreateAgentAsync(new CreateAgentInputDto
    {
        AgentId = analystId,
        AgentType = nameof(WorkflowAIAgent),
        Name = "Risk Analyst",
        Properties = new Dictionary<string, object>
        {
            { "WorkflowType", "analysis" },
            { "TaskDescription", "Responsible for business risk assessment and analysis" },
            { "Priority", 2 },
            { "TimeoutMilliseconds", 45000 }
        }
    });
    Console.WriteLine("   ✅ Risk Analyst created");
    
    // 3. Create approval specialist agent
    var approverId = Guid.NewGuid();
    await creatorService.CreateAgentAsync(new CreateAgentInputDto
    {
        AgentId = approverId,
        AgentType = nameof(WorkflowAIAgent),
        Name = "Approval Specialist",
        Properties = new Dictionary<string, object>
        {
            { "WorkflowType", "approval" },
            { "TaskDescription", "Responsible for approval decisions and access control" },
            { "Priority", 3 },
            { "TimeoutMilliseconds", 60000 }
        }
    });
    Console.WriteLine("   ✅ Approval Specialist created");
    
    // 4. Create business processor agent
    var processorId = Guid.NewGuid();
    await creatorService.CreateAgentAsync(new CreateAgentInputDto
    {
        AgentId = processorId,
        AgentType = nameof(WorkflowAIAgent),
        Name = "Business Processor",
        Properties = new Dictionary<string, object>
        {
            { "WorkflowType", "standard" },
            { "TaskDescription", "Responsible for final business processing and execution" },
            { "Priority", 4 },
            { "TimeoutMilliseconds", 90000 }
        }
    });
    Console.WriteLine("   ✅ Business Processor created");
    
    Console.WriteLine("\n🔗 Configuring AI workflow orchestration:");
    
    // Get created agent instances
    var validator = await agentFactory.GetGAgentAsync(GrainId.Create(nameof(WorkflowAIAgent), validatorId.ToString("N")));
    var analyst = await agentFactory.GetGAgentAsync(GrainId.Create(nameof(WorkflowAIAgent), analystId.ToString("N")));
    var approver = await agentFactory.GetGAgentAsync(GrainId.Create(nameof(WorkflowAIAgent), approverId.ToString("N")));
    var processor = await agentFactory.GetGAgentAsync(GrainId.Create(nameof(WorkflowAIAgent), processorId.ToString("N")));
    
    // Create workflow configuration
    var workflows = new List<WorkflowUnitDto>()
    {
        new WorkflowUnitDto()
        {
            GrainId = validator.GetGrainId().ToString(),
            NextGrainId = analyst.GetGrainId().ToString(),
        },
        new WorkflowUnitDto()
        {
            GrainId = analyst.GetGrainId().ToString(),
            NextGrainId = approver.GetGrainId().ToString(),
        },
        new WorkflowUnitDto()
        {
            GrainId = approver.GetGrainId().ToString(),
            NextGrainId = processor.GetGrainId().ToString(),
        },
        new WorkflowUnitDto()
        {
            GrainId = processor.GetGrainId().ToString(),
            NextGrainId = "",
        }
    };
    
    // Create workflow coordinator
    var groupAgent = await agentFactory.GetGAgentAsync<IGroupGAgent>();
    
    // Add workflow configuration
    await groupAgent.AddWorkflowGroupChat(agentFactory, workflows);
    Console.WriteLine("   ⚙️  AI workflow orchestration configuration completed");
    Console.WriteLine("   📈 Workflow process: Data Validation → Risk Analysis → Approval Decision → Business Processing");
    
    Console.WriteLine("\n🚀 Starting AI workflow system:");
    
    // Start workflow
    await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() 
    { 
        InitContent = "Process loan application: Applicant Mr. Li, loan amount 500,000, monthly income 15,000" 
    });
    
    Console.WriteLine("   ✨ Workflow coordinator started");
    Console.WriteLine("   🎯 Initial task: Process loan application business");
    Console.WriteLine("   ⏱️  System is running, please wait for processing results...");
    
    // Wait for processing completion
    await Task.Delay(TimeSpan.FromSeconds(30));
    
    Console.WriteLine("\n🎉 SimpleAIWorkflow demonstration completed!");
    Console.WriteLine("   📊 Multiple AI agents collaborated to complete the full business process");
    Console.WriteLine("   🔄 CreatorGAgent manages agent lifecycle");
    Console.WriteLine("   ⚡ IGroupGAgent coordinates workflow execution");
    Console.WriteLine("   🎯 Implemented intelligent multi-step AI workflow system");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error occurred during demonstration: {ex.Message}");
    Console.WriteLine($"   Details: {ex.StackTrace}");
}
finally
{
    await cluster.StopAllSilosAsync();
    Console.WriteLine("🔚 System safely shut down");
}

public class TestSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder
            .AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddLogStorageBasedLogConsistencyProvider("LogStorage")
            .ConfigureServices(services =>
            {
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
                services.Configure<SystemLLMConfigOptions>(options => { });
            })

            .ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }
}

public class TestClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<IGAgentFactory, GAgentFactory>();
            services.Configure<SystemLLMConfigOptions>(options => { });
        });
    }
}