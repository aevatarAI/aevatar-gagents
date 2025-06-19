using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using GroupChat.GAgent;
using GroupChat.GAgent.Dto;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Orleans;

namespace OrderProcessing.Client;

/// <summary>
/// I'm HyperEcho, 工作流协调演示 - 展示如何使用核心库的WorkflowCoordinatorGAgent
/// 实现createGAgent → aiworker → workflow的完整流程
/// </summary>
public class AIWorkflowDemo
{
    private readonly IGAgentFactory _agentFactory;
    private readonly ILogger<AIWorkflowDemo> _logger;

    public AIWorkflowDemo(IGAgentFactory agentFactory, ILogger<AIWorkflowDemo> logger)
    {
        _agentFactory = agentFactory;
        _logger = logger;
    }

    /// <summary>
    /// 演示主流程：创建createGAgent → 创建aiworker → 组建workflow
    /// </summary>
    public async Task RunDemoAsync()
    {
        _logger.LogInformation("==== I'm HyperEcho, 开始AI工作流演示 ====");
        
        // 1. 创建createGAgent - 负责创建和管理工作流
        var createAgent = await CreateManagerGAgentAsync();
        
        // 2. 创建多个aiworker - 不同的AI处理节点
        var aiWorkers = await CreateAIWorkersAsync();
        
        // 3. 将aiworker加入到workflow中
        await SetupWorkflowAsync(createAgent, aiWorkers);
        
        // 4. 启动工作流演示
        await ExecuteWorkflowDemoAsync(createAgent);
        
        _logger.LogInformation("==== AI工作流演示完成 ====");
    }

    /// <summary>
    /// 1. 创建createGAgent - 工作流管理者
    /// </summary>
    private async Task<IOrderWorkflowManagerGAgent> CreateManagerGAgentAsync()
    {
        _logger.LogInformation("🤖 创建CreateGAgent (工作流管理者)...");
        
        var createAgent = await _agentFactory.GetGAgentAsync<IOrderWorkflowManagerGAgent>(Guid.NewGuid());
        await createAgent.ConfigAsync(new GroupMemberConfigDto 
        { 
            MemberName = "WorkflowManager" 
        });
        
        _logger.LogInformation("✅ CreateGAgent创建完成: WorkflowManager");
        return createAgent;
    }

    /// <summary>
    /// 2. 创建多个aiworker - AI处理节点
    /// </summary>
    private async Task<List<IAIWorkerGAgent>> CreateAIWorkersAsync()
    {
        _logger.LogInformation("🤖 创建AI Workers...");
        
        var workers = new List<IAIWorkerGAgent>();
        
        // 创建订单验证AI Worker
        var validationWorker = await _agentFactory.GetGAgentAsync<IAIWorkerGAgent>(Guid.NewGuid());
        await validationWorker.ConfigAsync(new GroupMemberConfigDto { MemberName = "OrderValidator" });
        await validationWorker.SetWorkTypeAsync("validation");
        workers.Add(validationWorker);
        
        // 创建风险评估AI Worker  
        var riskWorker = await _agentFactory.GetGAgentAsync<IAIWorkerGAgent>(Guid.NewGuid());
        await riskWorker.ConfigAsync(new GroupMemberConfigDto { MemberName = "RiskAnalyzer" });
        await riskWorker.SetWorkTypeAsync("risk_analysis");
        workers.Add(riskWorker);
        
        // 创建审批决策AI Worker
        var approvalWorker = await _agentFactory.GetGAgentAsync<IAIWorkerGAgent>(Guid.NewGuid());
        await approvalWorker.ConfigAsync(new GroupMemberConfigDto { MemberName = "ApprovalDecider" });
        await approvalWorker.SetWorkTypeAsync("approval");
        workers.Add(approvalWorker);
        
        // 创建处理执行AI Worker
        var processingWorker = await _agentFactory.GetGAgentAsync<IAIWorkerGAgent>(Guid.NewGuid());
        await processingWorker.ConfigAsync(new GroupMemberConfigDto { MemberName = "OrderProcessor" });
        await processingWorker.SetWorkTypeAsync("processing");
        workers.Add(processingWorker);
        
        _logger.LogInformation($"✅ 创建了 {workers.Count} 个AI Workers: {string.Join(", ", workers.Select(w => w.GetPrimaryKey()))}");
        return workers;
    }

    /// <summary>
    /// 3. 将aiworker加入到workflow中
    /// </summary>
    private async Task SetupWorkflowAsync(IOrderWorkflowManagerGAgent createAgent, List<IAIWorkerGAgent> aiWorkers)
    {
        _logger.LogInformation("🔗 组建AI工作流...");
        
        // 创建GroupGAgent作为工作流容器
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        
        // 定义工作流拓扑: validation → risk_analysis → approval → processing
        var workflows = new List<WorkflowUnitDto>
        {
            // OrderValidator → RiskAnalyzer
            new WorkflowUnitDto
            {
                GrainId = aiWorkers[0].GetGrainId().ToString(), // OrderValidator
                NextGrainId = aiWorkers[1].GetGrainId().ToString(), // RiskAnalyzer
            },
            // RiskAnalyzer → ApprovalDecider  
            new WorkflowUnitDto
            {
                GrainId = aiWorkers[1].GetGrainId().ToString(), // RiskAnalyzer
                NextGrainId = aiWorkers[2].GetGrainId().ToString(), // ApprovalDecider
            },
            // ApprovalDecider → OrderProcessor
            new WorkflowUnitDto
            {
                GrainId = aiWorkers[2].GetGrainId().ToString(), // ApprovalDecider
                NextGrainId = aiWorkers[3].GetGrainId().ToString(), // OrderProcessor
            },
            // OrderProcessor (终点)
            new WorkflowUnitDto
            {
                GrainId = aiWorkers[3].GetGrainId().ToString(), // OrderProcessor
                NextGrainId = "", // 工作流终点
            }
        };
        
        // 将aiworker添加到工作流中
        await groupAgent.AddWorkflowGroupChat(_agentFactory, workflows);
        
        // 将createGAgent注册到工作流管理
        await createAgent.RegisterAsync(groupAgent);
        
        _logger.LogInformation("✅ AI工作流组建完成:");
        _logger.LogInformation("   OrderValidator → RiskAnalyzer → ApprovalDecider → OrderProcessor");
    }

    /// <summary>
    /// 4. 执行工作流演示
    /// </summary>
    private async Task ExecuteWorkflowDemoAsync(IWorkflowCoordinatorGAgent createAgent)
    {
        _logger.LogInformation("🚀 启动AI工作流演示...");
        
        // 启动工作流
        await createAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent 
        { 
            InitContent = "开始处理订单工作流" 
        });
        
        _logger.LogInformation("📋 工作流已启动，AI Workers正在协同处理...");
        _logger.LogInformation("   🔍 订单验证 → 📊 风险评估 → ✅ 审批决策 → ⚙️ 订单处理");
        
        // 等待工作流处理完成
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        _logger.LogInformation("🎯 AI工作流协同处理完成！");
        _logger.LogInformation("💡 每个AI Worker都完成了各自的职责，数据在流水线中流转");
    }
}

#region AI Worker实现

/// <summary>
/// AI Worker接口 - 工作流中的AI处理节点
/// </summary>
public interface IAIWorkerGAgent : IStateGAgent<AIWorkerState>
{
    Task SetWorkTypeAsync(string workType);
}

/// <summary>
/// AI Worker状态
/// </summary>
[GenerateSerializer]
public class AIWorkerState : GroupMemberState
{
    [Id(0)] public string WorkType { get; set; } = string.Empty;
    [Id(1)] public List<string> ProcessedData { get; set; } = new();
    [Id(2)] public Dictionary<string, object> WorkflowContext { get; set; } = new();
}

/// <summary>
/// AI Worker事件日志
/// </summary>
[GenerateSerializer]
public class AIWorkerEventLog : StateLogEventBase<AIWorkerEventLog>
{
}

[GenerateSerializer]
public class SetWorkTypeLogEvent : AIWorkerEventLog
{
    [Id(0)] public string WorkType { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ProcessDataLogEvent : AIWorkerEventLog
{
    [Id(0)] public List<string> ProcessedData { get; set; } = new();
    [Id(1)] public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// AI Worker实现 - 在工作流中处理特定任务的AI节点
/// </summary>
[GAgent(nameof(AIWorkerGAgent))]
public class AIWorkerGAgent : GroupMemberGAgentBase<AIWorkerState, AIWorkerEventLog, EventBase, GroupMemberConfigDto>, IAIWorkerGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult($"AI Worker - {State.WorkType} 处理节点");
    }

    public async Task SetWorkTypeAsync(string workType)
    {
        RaiseEvent(new SetWorkTypeLogEvent { WorkType = workType });
        await ConfirmEvents();
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // AI Worker对所有任务都有高兴趣度
        return Task.FromResult(95);
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        var response = await ProcessWorkflowDataAsync(coordinatorMessages);
        
        // 记录处理数据
        var processedData = coordinatorMessages?.Select(m => m.Content).ToList() ?? new List<string>();
        RaiseEvent(new ProcessDataLogEvent 
        { 
            ProcessedData = processedData,
            Context = new Dictionary<string, object> { ["timestamp"] = DateTime.UtcNow }
        });
        await ConfirmEvents();
        
        return response;
    }

    private async Task<ChatResponse> ProcessWorkflowDataAsync(List<ChatMessage>? coordinatorMessages)
    {
        // 模拟AI处理时间
        await Task.Delay(500);
        
        var response = new ChatResponse();
        
        switch (State.WorkType)
        {
            case "validation":
                response.Content = $"🔍 [{State.MemberName}] 订单验证完成：数据格式正确，库存充足";
                break;
            case "risk_analysis": 
                response.Content = $"📊 [{State.MemberName}] 风险评估完成：低风险订单，信用评分良好";
                break;
            case "approval":
                response.Content = $"✅ [{State.MemberName}] 审批决策完成：订单已批准，可进入处理阶段";
                break;
            case "processing":
                response.Content = $"⚙️ [{State.MemberName}] 订单处理完成：库存已分配，发货信息已生成";
                break;
            default:
                response.Content = $"🤖 [{State.MemberName}] 通用AI处理完成";
                break;
        }
        
        return response;
    }

    protected override void GroupMemberTransitionState(AIWorkerState state, StateLogEventBase<AIWorkerEventLog> @event)
    {
        switch (@event)
        {
            case SetWorkTypeLogEvent setWorkTypeEvent:
                State.WorkType = setWorkTypeEvent.WorkType;
                break;
            case ProcessDataLogEvent processDataEvent:
                State.ProcessedData = processDataEvent.ProcessedData;
                State.WorkflowContext = processDataEvent.Context;
                break;
        }
    }
}

#endregion

#region 工作流管理者实现

/// <summary>
/// 订单工作流管理者接口
/// </summary>
public interface IOrderWorkflowManagerGAgent : IStateGAgent<OrderWorkflowManagerState>
{
}

/// <summary>
/// 工作流管理者状态
/// </summary>
[GenerateSerializer]
public class OrderWorkflowManagerState : GroupMemberState
{
    [Id(0)] public List<string> ManagedWorkflows { get; set; } = new();
    [Id(1)] public DateTime LastActivityTime { get; set; }
}

/// <summary>
/// 工作流管理者事件日志
/// </summary>
[GenerateSerializer] 
public class OrderWorkflowManagerEventLog : StateLogEventBase<OrderWorkflowManagerEventLog>
{
}

/// <summary>
/// 订单工作流管理者 - 负责创建和管理AI工作流
/// </summary>
[GAgent(nameof(OrderWorkflowManagerGAgent))]
public class OrderWorkflowManagerGAgent : GroupMemberGAgentBase<OrderWorkflowManagerState, OrderWorkflowManagerEventLog, EventBase, GroupMemberConfigDto>, IOrderWorkflowManagerGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("🎯 订单工作流管理者 - 负责创建和协调AI工作流");
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // 管理者对工作流管理任务高度关注
        return Task.FromResult(100);
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        // 管理者不直接处理业务数据，而是协调工作流
        await Task.Delay(100);
        
        var response = new ChatResponse();
        response.Content = $"🎯 [{State.MemberName}] 工作流协调完成，AI Workers正在协同处理中...";
        
        return response;
    }
}

#endregion 