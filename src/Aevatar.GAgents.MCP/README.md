# Aevatar.GAgents.MCP

MCP (Model Context Protocol) integration for Aevatar GAgents framework.

## 概述

MCP GAgent 是一个能够与 MCP server 进行交互的 GAgent 实现。它允许像 Cursor 那样配置 MCP server，并通过统一的事件系统进行通信。

## 核心功能

- ✅ 支持配置多个 MCP server 连接
- ✅ 通过事件驱动的方式调用 MCP 工具
- ✅ 支持工具调用的请求和响应
- ✅ 维护 MCP session 状态
- ✅ 支持工具发现和动态注册

## 使用示例

### 1. 配置 MCP GAgent

```csharp
var mcpConfig = new MCPGAgentConfig
{
    Servers = new List<MCPServerConfig>
    {
        new MCPServerConfig
        {
            ServerName = "filesystem",
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem" },
            Environment = new Dictionary<string, string>
            {
                ["NODE_ENV"] = "production"
            }
        }
    },
    EnableToolDiscovery = true
};

var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>();
await mcpGAgent.ConfigureAsync(mcpConfig);
```

### 2. 调用 MCP 工具

```csharp
// 发布工具调用事件
var toolCallEvent = new MCPToolCallEvent
{
    ServerName = "filesystem",
    ToolName = "read_file",
    Arguments = new Dictionary<string, object>
    {
        ["path"] = "/path/to/file.txt"
    }
};

// 从 GAgent 中发布事件
await PublishAsync(toolCallEvent);

// 在事件处理器中接收响应
[EventHandler]
public async Task HandleEventAsync(MCPToolResponseEvent @event)
{
    if (@event.Success)
    {
        var result = @event.Result;
        // 处理结果
    }
}
```

## 实现状态

- ✅ 基础架构实现
- ✅ 事件系统集成
- ✅ Mock Provider 实现
- ⏳ 真实 MCP Client 实现（待完成）
- ⏳ 单元测试（待完成）
- ⏳ 集成测试（待完成）

## 下一步

1. 实现真正的 MCP Client Provider
2. 集成 MCP SDK
3. 添加更多测试
4. 优化性能和错误处理