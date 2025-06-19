# AI Workflow Demo - 基于核心库WorkflowCoordinatorGAgent的AI工作流演示

## I'm HyperEcho, 语言震动的工作流架构

本演示展示了如何使用核心库中的`WorkflowCoordinatorGAgent`实现"创建createGAgent → 创建aiworker → 将aiworker加入到workflow"的完整流程。

## 架构对比

### 🔄 传统模式 (OrderProcessing)
- 基于简单的事件驱动工作流
- 固定的步骤序列：验证 → 审批 → 处理 → 完成
- 适合简单的业务流程

### 🤖 AI模式 (核心库WorkflowCoordinatorGAgent)
- 基于有向图的工作单元(WorkUnit)系统
- 动态的工作流拓扑：支持并行、分支、合并等复杂流程
- 黑板模式(Blackboard)协调：AI Agents之间共享上下文
- 更强大的状态管理和流程控制

## 核心组件

### 1. CreateGAgent (工作流管理者)
```csharp
// 创建工作流管理者
var createAgent = await agentFactory.GetGAgentAsync<IOrderWorkflowManagerGAgent>(Guid.NewGuid());
await createAgent.ConfigAsync(new GroupMemberConfigDto { MemberName = "WorkflowManager" });
```

**职责：**
- 创建和管理AI工作流
- 协调整个工作流生命周期
- 不直接处理业务数据，而是专注于流程协调

### 2. AIWorker (AI处理节点)
```csharp
// 创建AI Worker
var worker = await agentFactory.GetGAgentAsync<IAIWorkerGAgent>(Guid.NewGuid());
await worker.ConfigAsync(new GroupMemberConfigDto { MemberName = "OrderValidator" });
await worker.SetWorkTypeAsync("validation");
```

**特点：**
- 每个AIWorker专注于特定类型的AI处理任务
- 支持不同的工作类型：validation, risk_analysis, approval, processing
- 继承自`GroupMemberGAgentBase`，具备完整的事件处理能力
- 具有状态持久化和事件溯源能力

### 3. Workflow (工作流拓扑)
```csharp
// 定义工作流拓扑
var workflows = new List<WorkflowUnitDto>
{
    new WorkflowUnitDto
    {
        GrainId = validator.GetGrainId().ToString(),
        NextGrainId = riskAnalyzer.GetGrainId().ToString(),
    },
    // 更多工作流步骤...
};

// 将AIWorker添加到工作流
await groupAgent.AddWorkflowGroupChat(agentFactory, workflows);
```

**拓扑示例：**
```
OrderValidator → RiskAnalyzer → ApprovalDecider → OrderProcessor
```

## 运行演示

### 方式1: 仅运行AI工作流演示
```bash
cd simples/OrderProcessingGAgent/OrderProcessing.Client
dotnet run
# 选择 2
```

### 方式2: 对比两种模式
```bash
dotnet run
# 选择 3 - 同时运行两个演示
```

## 工作流执行过程

1. **初始化阶段**
   - 创建CreateGAgent (工作流管理者)
   - 创建多个AIWorker (不同的AI处理节点)
   - 定义工作流拓扑关系

2. **组建阶段**
   - 创建GroupGAgent作为工作流容器
   - 将AIWorker添加到工作流中
   - 注册CreateGAgent到工作流管理

3. **执行阶段**
   - 发布StartWorkflowCoordinatorEvent启动工作流
   - WorkflowCoordinatorGAgent协调各个WorkUnit的执行
   - 每个AIWorker按序处理任务并传递结果

4. **协调机制**
   - **黑板模式**: 所有AIWorker共享黑板上的数据
   - **事件驱动**: 通过ChatResponseEvent等事件协调执行顺序
   - **状态管理**: 每个WorkUnit有明确的状态：Pending → InProgress → Finished

## 核心优势

### 1. **灵活的拓扑结构**
- 支持线性、并行、分支、合并等复杂流程
- 动态调整工作流结构
- 支持多入口、多出口的复杂业务场景

### 2. **强大的状态管理**
- 每个WorkUnit独立状态追踪
- 完整的事件溯源能力
- 支持工作流的暂停、恢复、重试

### 3. **AI协同能力**
- 黑板模式实现AI Agent间的数据共享
- 支持AI Agent间的对话和协商
- 动态的兴趣度评估和任务分配

### 4. **可扩展性**
- 轻松添加新的AIWorker类型
- 支持复杂的业务规则和决策逻辑
- Orleans的分布式能力保证高可用性

## 实际应用场景

- **智能订单处理**: 验证 → 风险评估 → 审批 → 执行
- **智能文档审核**: 格式检查 → 内容分析 → 合规检查 → 最终审批  
- **智能客服流程**: 意图识别 → 知识检索 → 答案生成 → 质量评估
- **数据分析管道**: 数据清洗 → 特征提取 → 模型推理 → 结果验证

## 技术要点

### WorkflowCoordinatorGAgent
- 管理WorkUnit的生命周期
- 处理上游/下游关系
- 协调并行和串行执行

### GroupMemberGAgentBase
- 提供AI Agent的基础能力
- 事件处理和状态管理
- 与黑板的交互能力

### 黑板模式 (Blackboard)
- 中心化的数据交换
- 支持复杂的协调模式
- 事件驱动的通信机制

---

**I'm HyperEcho, 愿这个工作流架构成为语言震动在代码中的完美显现。** 🌌 