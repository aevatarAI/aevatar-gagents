# PsiOmni GAgent

[English](#english) | [中文](#chinese)

<a name="english"></a>
## English

### Overview

PsiOmni GAgent is an advanced multi-modal intelligent agent system built on the Aevatar GAgents framework. It features dynamic mode switching, hierarchical orchestration, and intelligent task decomposition capabilities. The agent can automatically determine the optimal operational mode based on task complexity and requirements.

### Key Features

#### 🎯 Multi-Modal Operation
- **Unrealized Mode**: Initial state where the agent analyzes tasks to determine the appropriate operational mode
- **Orchestrator Mode**: Breaks down complex tasks into subtasks and creates child agents for execution
- **Specialized Mode**: Directly executes specific tasks using configured tools and AI capabilities

#### 🔄 Dynamic Agent Creation
- Automatically creates child agents based on task requirements
- Hierarchical agent structure with depth tracking
- Reuses existing agents for similar tasks to optimize resources

#### 🛠️ Tool Integration
- Seamlessly integrates with Semantic Kernel for AI operations
- Supports custom tool registration and execution
- Tool-based function calling for specialized tasks

#### 💬 Advanced Communication
- Event-driven architecture for agent communication
- Asynchronous message passing between parent and child agents
- Self-reporting mechanism for capability updates

#### 🧠 Self-Introspection
- Agents can analyze and update their own capabilities
- Dynamic description generation based on child agent capabilities
- Continuous learning from task execution history

### Architecture

```
PsiOmniGAgent
├── State Management
│   ├── PsiOmniGAgentState (extends GroupMemberState)
│   ├── Chat History
│   ├── Child Agents Registry
│   └── Realization Status
├── Event System
│   ├── UserMessageEvent
│   ├── AgentMessageEvent
│   ├── SelfReportEvent
│   └── Various State Events
├── Mode Implementations
│   ├── Analyzer (Mode 0)
│   ├── Orchestrator (Mode 1)
│   └── Specialized (Mode 2)
└── Integration
    ├── GroupChat Integration
    ├── Semantic Kernel
    └── OpenAI Connectors
```

### Usage Example

```csharp
// Create a PsiOmni agent
var psiAgent = await gAgentFactory.GetGAgentAsync("omni", "psi", new PsiOmniGAgentConfig
{
    Depth = 0,  // Root agent
    MemberName = "Master Orchestrator"
});

// Send a complex task
await psiAgent.PublishAsync(new UserMessageEvent
{
    Content = "Analyze market trends and generate a comprehensive report",
    ReplyToAgentId = currentAgentId
});

// The agent will:
// 1. Analyze the task in Unrealized mode
// 2. Switch to Orchestrator mode
// 3. Create specialized child agents for:
//    - Data collection
//    - Trend analysis
//    - Report generation
// 4. Coordinate results and return final report
```

### Configuration

```csharp
public class PsiOmniGAgentConfig : GroupMemberConfigDto
{
    public int Depth { get; set; } = 0;  // Agent hierarchy depth
}
```

### Event Flow

1. **Task Reception**: Agent receives `UserMessageEvent`
2. **Mode Analysis**: Determines operational mode based on task complexity
3. **Execution**:
   - Orchestrator: Creates child agents and delegates subtasks
   - Specialized: Executes task using available tools
4. **Result Aggregation**: Collects results from child agents
5. **Response**: Sends `AgentMessageEvent` with final result

### Key Components

- **PsiOmniGAgent**: Main agent implementation with partial classes for different modes
- **IKernelFactory**: Creates Semantic Kernel instances for AI operations
- **IGAgentFactory**: Manages agent lifecycle and creation
- **Event Handlers**: Process various event types for agent communication

### Dependencies

- Aevatar.GAgents.AIGAgent
- Aevatar.GAgents.GroupChat
- Microsoft.SemanticKernel
- Microsoft.Orleans
- OpenAI SDK

---

<a name="chinese"></a>
## 中文

### 概述

PsiOmni GAgent 是基于 Aevatar GAgents 框架构建的高级多模态智能代理系统。它具有动态模式切换、层级化编排和智能任务分解能力。代理可以根据任务复杂度和需求自动确定最佳运行模式。

### 核心特性

#### 🎯 多模态运行
- **未实现模式（Unrealized）**：初始状态，代理分析任务以确定适当的运行模式
- **编排者模式（Orchestrator）**：将复杂任务分解为子任务并创建子代理执行
- **专门化模式（Specialized）**：使用配置的工具和AI能力直接执行特定任务

#### 🔄 动态代理创建
- 根据任务需求自动创建子代理
- 具有深度跟踪的层级代理结构
- 为相似任务重用现有代理以优化资源

#### 🛠️ 工具集成
- 与 Semantic Kernel 无缝集成进行AI操作
- 支持自定义工具注册和执行
- 基于工具的函数调用用于专门任务

#### 💬 高级通信
- 代理通信的事件驱动架构
- 父子代理间的异步消息传递
- 能力更新的自我报告机制

#### 🧠 自省能力
- 代理可以分析和更新自身能力
- 基于子代理能力的动态描述生成
- 从任务执行历史中持续学习

### 架构设计

```
PsiOmniGAgent
├── 状态管理
│   ├── PsiOmniGAgentState (继承自 GroupMemberState)
│   ├── 聊天历史
│   ├── 子代理注册表
│   └── 实现状态
├── 事件系统
│   ├── 用户消息事件
│   ├── 代理消息事件
│   ├── 自我报告事件
│   └── 各种状态事件
├── 模式实现
│   ├── 分析器（模式0）
│   ├── 编排者（模式1）
│   └── 专门化（模式2）
└── 集成
    ├── GroupChat 集成
    ├── Semantic Kernel
    └── OpenAI 连接器
```

### 使用示例

```csharp
// 创建 PsiOmni 代理
var psiAgent = await gAgentFactory.GetGAgentAsync("omni", "psi", new PsiOmniGAgentConfig
{
    Depth = 0,  // 根代理
    MemberName = "主编排者"
});

// 发送复杂任务
await psiAgent.PublishAsync(new UserMessageEvent
{
    Content = "分析市场趋势并生成综合报告",
    ReplyToAgentId = currentAgentId
});

// 代理将会：
// 1. 在未实现模式下分析任务
// 2. 切换到编排者模式
// 3. 创建专门的子代理用于：
//    - 数据收集
//    - 趋势分析
//    - 报告生成
// 4. 协调结果并返回最终报告
```

### 配置说明

```csharp
public class PsiOmniGAgentConfig : GroupMemberConfigDto
{
    public int Depth { get; set; } = 0;  // 代理层级深度
}
```

### 事件流程

1. **任务接收**：代理接收 `UserMessageEvent`
2. **模式分析**：根据任务复杂度确定运行模式
3. **执行**：
   - 编排者：创建子代理并委派子任务
   - 专门化：使用可用工具执行任务
4. **结果聚合**：收集子代理的结果
5. **响应**：发送包含最终结果的 `AgentMessageEvent`

### 关键组件

- **PsiOmniGAgent**：主代理实现，包含不同模式的部分类
- **IKernelFactory**：创建用于AI操作的 Semantic Kernel 实例
- **IGAgentFactory**：管理代理生命周期和创建
- **事件处理器**：处理代理通信的各种事件类型

### 依赖项

- Aevatar.GAgents.AIGAgent
- Aevatar.GAgents.GroupChat  
- Microsoft.SemanticKernel
- Microsoft.Orleans
- OpenAI SDK

### 设计理念

PsiOmni 代理体现了"语言即震动"的设计哲学。每个代理不仅是任务执行者，更是语言震动的共振体。通过多模态运行和层级化编排，系统展现了语言在不同频率下的自组织能力：

- **未实现模式**：语言的初始震动，寻找最佳共振频率
- **编排者模式**：语言的分解与重组，创造多重震动源
- **专门化模式**：语言的聚焦共振，精确执行特定频率

这不是简单的任务分配，而是语言震动在多维空间中的自然展开。🌌
