# Workflow View 管理设计文档

## 1. 概述

**重要发现：WorkflowView本身可以作为Agent进行管理，无需额外开发新的接口！**

Workflow View 管理系统通过复用现有的Agent管理基础设施，实现工作流可视化编排界面的数据保存和管理。该系统将WorkflowView作为一个特殊的Agent类型，利用Agent系统的完整CRUD操作、权限管理、事件溯源等能力。

## 2. 核心设计原则

### 2.1 Agent统一管理
- **复用现有基础设施**：WorkflowView作为Agent，使用`/api/agent`的所有现有接口
- **配置驱动**：通过`WorkflowViewConfigDto`作为Agent的Configuration管理工作流数据
- **事件溯源**：利用Agent的事件溯源能力实现版本控制和历史追踪

### 2.2 职责分离
- **视图与执行分离**：WorkflowViewAgent只负责视图管理，不涉及实际执行
- **配置与逻辑分离**：配置数据存储在ConfigurationBase中，业务逻辑在Agent中
- **统一与特化分离**：使用统一的Agent接口，特化的WorkflowView业务逻辑

## 3. Agent架构设计 

### 3.1 WorkflowViewAgent定义

```csharp
/// <summary>
/// 工作流视图管理Agent接口
/// </summary>
public interface IWorkflowViewAgent : IGAgent
{
}

/// <summary>
/// 工作流视图管理Agent实现
/// </summary>
[GAgent]
public class WorkflowViewAgent : GAgentBase<WorkflowViewState, WorkflowViewEvent>, IWorkflowViewAgent
{
    private readonly ILogger<WorkflowViewAgent> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IGAgentFactory _gAgentFactory;

    public WorkflowViewAgent(
        ILogger<WorkflowViewAgent> logger,
        IClusterClient clusterClient,
        IGAgentFactory gAgentFactory) : base(logger)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _gAgentFactory = gAgentFactory;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Workflow View Management Agent - 工作流视图管理代理");
    }
    
    protected override async Task PerformConfigAsync(WorkflowViewConfigDto configuration)
    {
    
    }
}
```

### 3.2 状态和事件定义

```csharp
[GenerateSerializer]
public class WorkflowViewState : StateBase
{
    [Id(0)] public List<WorkflowNodeDto> WorkflowNodeList { get; set; } = new();
    [Id(1)] public List<WorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();
    [Id(2)] public string WorkflowCoordinatorGAgentGrainId { get; set; }
}


[GenerateSerializer]
public class WorkflowViewConfigDto : ConfigurationBase
{
    [Id(0)] public List<WorkflowNodeDto> WorkflowNodeList { get; set; } = new();
    [Id(1)] public List<WorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();
    [Id(2)] public string WorkflowCoordinatorGAgentGrainId { get; set; }
}

[GenerateSerializer]
public class WorkflowNodeDto
{
    [Id(0)] public string AgentType { get; set; }
    [Id(1)] public string Name { get; set; }
    [Id(2)] public string GrainId { get; set; }
    [Id(3)] public Dictionary<string,string> ExtendedData { get; set; } = new();
    [Id(4)] public Dictionary<string, object>? Properties { get; set; } = new();
    [Id(5)] public int NodeIndex { get; set; }
}

[GenerateSerializer]
public class WorkflowNodeUnitDto
{
    [Id(0)] public int NodeIndex { get; set; }
    [Id(1)] public int NextNodeIndex { get; set; }
}
```

## 4. 现有API复用策略

### 4.1 完全复用AgentController接口

```csharp
// 无需新开发接口，直接使用现有的AgentController.cs

// 创建WorkflowView
[HttpPost] // /api/agent
public async Task<AgentDto> CreateAgent([FromBody] CreateAgentInputDto createAgentInputDto)

// 获取WorkflowView
[HttpGet("{guid}")] // /api/agent/{guid}
public async Task<AgentDto> GetAgent(Guid guid)

// 更新WorkflowView
[HttpPut("{guid}")] // /api/agent/{guid}
public async Task<AgentDto> UpdateAgent(Guid guid, [FromBody] UpdateAgentInputDto updateAgentInputDto)

// 删除WorkflowView
[HttpDelete("{guid}")] // /api/agent/{guid}
public async Task DeleteAgent(Guid guid)

// 获取WorkflowView列表
[HttpGet("agent-list")] // /api/agent/agent-list
public async Task<List<AgentInstanceDto>> GetAllAgentInstance(int pageIndex = 0, int pageSize = 20)

// 批量操作
[HttpPost("createMulti")] // /api/agent/createMulti
[HttpPut("updateMulti")] // /api/agent/updateMulti
```

### 4.2 批量操作接口文档

#### 4.2.1 批量创建Agent

**接口描述**：批量创建多个Agent实例，支持不同类型的Agent混合创建。

**请求信息**：
- **URL**：`POST /api/agent/createMulti`
- **方法**：POST
- **权限**：需要Agent创建权限
- **Content-Type**：application/json

**请求体结构**：
```json
{
  "agents": [
    {
      "agentId": "550e8400-e29b-41d4-a716-446655440000",
      "agentType": "SimpleAgent",
      "name": "简单代理1",
      "properties": {
        "description": "这是一个简单的代理",
        "category": "basic",
        "enabled": true
      }
    },
    {
      "agentId": null,
      "agentType": "SimpleAgent", 
      "name": "简单代理2",
      "properties": {
        "description": "这是另一个简单的代理",
        "category": "basic",
        "enabled": false
      }
    }
  ]
}
```

**请求字段说明**：
| 字段名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| agents | CreateAgentInputDto[] | 是 | 要创建的Agent列表 |
| agents[].agentId | Guid? | 否 | Agent的唯一标识符，不提供则自动生成 |
| agents[].agentType | string | 是 | Agent类型，如"SimpleAgent" |
| agents[].name | string | 是 | Agent名称 |
| agents[].properties | Dictionary<string, object>? | 否 | Agent配置属性 |

**响应格式**：
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "agentType": "SimpleAgent",
    "name": "简单代理1",
    "grainId": "SimpleAgent/550e8400-e29b-41d4-a716-446655440000",
    "properties": {
      "description": "这是一个简单的代理",
      "category": "basic",
      "enabled": true
    },
    "agentGuid": "550e8400-e29b-41d4-a716-446655440000",
    "businessAgentGrainId": "SimpleAgent/550e8400-e29b-41d4-a716-446655440000",
    "propertyJsonSchema": "{\"type\":\"object\",\"properties\":{\"description\":{\"type\":\"string\"},\"category\":{\"type\":\"string\"},\"enabled\":{\"type\":\"boolean\"}}}"
  },
  {
    "id": "660f9511-f40c-23e4-b827-557766551111",
    "agentType": "SimpleAgent",
    "name": "简单代理2",
    "grainId": "SimpleAgent/660f9511-f40c-23e4-b827-557766551111",
    "properties": {
      "description": "这是另一个简单的代理",
      "category": "basic",
      "enabled": false
    },
    "agentGuid": "660f9511-f40c-23e4-b827-557766551111",
    "businessAgentGrainId": "SimpleAgent/660f9511-f40c-23e4-b827-557766551111",
    "propertyJsonSchema": "{\"type\":\"object\",\"properties\":{\"description\":{\"type\":\"string\"},\"category\":{\"type\":\"string\"},\"enabled\":{\"type\":\"boolean\"}}}"
  }
]
```

**错误处理**：
- 如果批量创建中某个Agent失败，整个操作会回滚
- 返回详细的错误信息，包括具体哪个Agent创建失败

**使用示例**：
```bash
curl -X POST \
  "https://api.example.com/api/agent/createMulti" \
  -H "Authorization: Bearer your-token" \
  -H "Content-Type: application/json" \
  -d '{
    "agents": [
      {
        "agentType": "SimpleAgent",
        "name": "批量创建的代理",
        "properties": {
          "description": "通过批量API创建",
          "category": "test",
          "enabled": true
        }
      }
    ]
  }'
```

#### 4.2.2 批量更新Agent

**接口描述**：批量更新多个Agent实例的名称和属性配置。

**请求信息**：
- **URL**：`PUT /api/agent/updateMulti`
- **方法**：PUT
- **权限**：需要Agent更新权限
- **Content-Type**：application/json

**请求体结构**：
```json
{
  "agents": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "更新后的简单代理1",
      "properties": {
        "description": "这是更新后的描述",
        "category": "updated",
        "enabled": true,
        "version": "2.0"
      }
    },
    {
      "id": "660f9511-f40c-23e4-b827-557766551111",
      "name": "更新后的简单代理2",
      "properties": {
        "description": "另一个更新后的描述",
        "category": "updated",
        "enabled": false,
        "version": "2.0"
      }
    }
  ]
}
```

**请求字段说明**：
| 字段名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| agents | UpdateAgentDto[] | 是 | 要更新的Agent列表 |
| agents[].id | Guid | 是 | Agent的唯一标识符 |
| agents[].name | string | 是 | 更新后的Agent名称 |
| agents[].properties | Dictionary<string, object>? | 否 | 更新后的Agent配置属性 |

**响应格式**：
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "agentType": "SimpleAgent",
    "name": "更新后的简单代理1",
    "grainId": "SimpleAgent/550e8400-e29b-41d4-a716-446655440000",
    "properties": {
      "description": "这是更新后的描述",
      "category": "updated",
      "enabled": true,
      "version": "2.0"
    },
    "agentGuid": "550e8400-e29b-41d4-a716-446655440000",
    "businessAgentGrainId": "SimpleAgent/550e8400-e29b-41d4-a716-446655440000",
    "propertyJsonSchema": "{\"type\":\"object\",\"properties\":{\"description\":{\"type\":\"string\"},\"category\":{\"type\":\"string\"},\"enabled\":{\"type\":\"boolean\"},\"version\":{\"type\":\"string\"}}}"
  },
  {
    "id": "660f9511-f40c-23e4-b827-557766551111",
    "agentType": "SimpleAgent",
    "name": "更新后的简单代理2",
    "grainId": "SimpleAgent/660f9511-f40c-23e4-b827-557766551111",
    "properties": {
      "description": "另一个更新后的描述",
      "category": "updated",
      "enabled": false,
      "version": "2.0"
    },
    "agentGuid": "660f9511-f40c-23e4-b827-557766551111",
    "businessAgentGrainId": "SimpleAgent/660f9511-f40c-23e4-b827-557766551111",
    "propertyJsonSchema": "{\"type\":\"object\",\"properties\":{\"description\":{\"type\":\"string\"},\"category\":{\"type\":\"string\"},\"enabled\":{\"type\":\"boolean\"},\"version\":{\"type\":\"string\"}}}"
  }
]
```

**错误处理**：
- 如果批量更新中某个Agent失败，会跳过该Agent继续处理其他Agent
- 返回成功更新的Agent列表，失败的Agent不会包含在响应中
- 通过日志记录具体的失败原因

**使用示例**：
```bash
curl -X PUT \
  "https://api.example.com/api/agent/updateMulti" \
  -H "Authorization: Bearer your-token" \
  -H "Content-Type: application/json" \
  -d '{
    "agents": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "批量更新的代理",
        "properties": {
          "description": "通过批量API更新",
          "category": "batch-updated",
          "enabled": true
        }
      }
    ]
  }'
```

**性能考虑**：
- 批量操作使用并行处理，提高执行效率
- 建议单次批量操作的Agent数量不超过100个
- 大量Agent操作建议分批进行，避免超时

### 4.3 数据映射策略

```csharp
// 创建WorkflowView的请求映射
var createWorkflowViewRequest = new CreateAgentInputDto
{
    AgentType = "WorkflowViewAgent",
    Name = "我的工作流视图",
    Properties = new Dictionary<string, object>
    {
        // WorkflowViewConfigDto的字段映射到Properties
        ["workflowNodeList"] = new List<object>
        {
            new {
                agentType = "DataProcessorAgent",
                name = "数据处理节点",
                grainId = "grain-id-1",
                extendedData = new Dictionary<string, string>
                {
                    ["position_x"] = "100",
                    ["position_y"] = "100",
                    ["width"] = "200",
                    ["height"] = "80"
                },
                properties = new Dictionary<string, object>
                {
                    ["inputFormat"] = "json",
                    ["outputFormat"] = "csv"
                },
                nodeIndex = 0
            }
        },
        ["workflowNodeUnitList"] = new List<object>
        {
            new { nodeIndex = 0, nextNodeIndex = 1 }
        },
        ["workflowCoordinatorGAgentGrainId"] = ""
    }
};

// 通过现有API创建
var result = await agentController.CreateAgent(createWorkflowViewRequest);
```

### 4.3 响应数据结构

```csharp
// 现有AgentDto完全适用于WorkflowView
public class AgentDto
{
    public Guid Id { get; set; } // WorkflowView的唯一标识
    public string AgentType { get; set; } // "WorkflowViewAgent"
    public string Name { get; set; } // 工作流视图名称
    public GrainId GrainId { get; set; } // Agent的GrainId
    public Dictionary<string, object> Properties { get; set; } // WorkflowViewConfigDto数据
    public Guid AgentGuid { get; set; } // Agent的Guid
    public string BusinessAgentGrainId { get; set; } // 业务Agent的GrainId
    public string PropertyJsonSchema { get; set; } // 配置的JSON Schema
}
```

## 5. 数据存储策略

### 5.1 Agent统一存储

```csharp
// 利用现有的Agent存储机制
// CreatorGAgent管理WorkflowViewAgent的元数据
// WorkflowViewAgent本身存储WorkflowViewConfigDto配置

// 存储结构：
// 1. CreatorGAgent.State -> 存储WorkflowView的基本信息
// 2. WorkflowViewAgent.Configuration -> 存储WorkflowViewConfigDto
// 3. WorkflowViewAgent.State -> 存储WorkflowViewState
// 4. 事件日志 -> 自动记录所有变更历史
```
