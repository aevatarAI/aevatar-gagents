using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// 测试专用的Mock Agent信息提供器
/// </summary>
public class MockAgentInfoProvider : IAgentInfoProvider
{
    private readonly List<AgentIndexInfo> _mockAgents;

    public MockAgentInfoProvider()
    {
        _mockAgents = CreateMockAgents();
    }

    public MockAgentInfoProvider(List<AgentIndexInfo> customAgents)
    {
        _mockAgents = customAgents;
    }

    public Task<List<AgentIndexInfo>> GetAllAgentsAsync()
    {
        return Task.FromResult(_mockAgents.ToList());
    }

    public Task<AgentIndexInfo?> GetAgentByIdAsync(string agentId)
    {
        var agent = _mockAgents.FirstOrDefault(a => a.Id == agentId);
        return Task.FromResult(agent);
    }

    public Task<List<AgentIndexInfo>> GetAgentsByCategoryAsync(string category)
    {
        var agents = _mockAgents.Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(agents);
    }

    public Task<List<AgentIndexInfo>> GetAgentsByCapabilityAsync(string capability)
    {
        var agents = _mockAgents
            .Where(a => a.Capabilities.Any(c => c.Contains(capability, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return Task.FromResult(agents);
    }

    public Task<AgentStatistics> GetStatisticsAsync()
    {
        var stats = new AgentStatistics
        {
            TotalAgents = _mockAgents.Count,
            LastUpdated = DateTime.UtcNow,
            CategoriesCount = _mockAgents.GroupBy(a => a.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            CapabilitiesCount = _mockAgents.SelectMany(a => a.Capabilities)
                .GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count())
        };
        return Task.FromResult(stats);
    }

    /// <summary>
    /// 创建测试用的Mock Agent数据
    /// </summary>
    private static List<AgentIndexInfo> CreateMockAgents()
    {
        return new List<AgentIndexInfo>
        {
            new()
            {
                Id = "MockChatAgent",
                Name = "模拟聊天代理",
                Category = "AI",
                L1Description = "用于测试的模拟聊天代理，支持基本对话功能",
                L2Description = "这是一个测试专用的模拟聊天代理，提供标准的对话接口用于单元测试和集成测试。支持预定义回复和状态管理。",
                Capabilities = new List<string> { "chat", "test-responses", "state-management" },
                Tags = new List<string> { "test", "mock", "chat", "ai" },
                InputFormat = "text",
                OutputFormat = "text",
                UsageExample = "var response = await mockAgent.ChatAsync('Hello');",
                AgentType = "MockChatAgent",
                InterfaceType = "IMockChatAgent"
            },
            new()
            {
                Id = "MockWorkflowAgent",
                Name = "模拟工作流代理",
                Category = "Workflow",
                L1Description = "用于测试的模拟工作流代理，支持基本流程编排",
                L2Description = "测试专用的工作流代理，提供简化的流程编排能力，用于验证工作流相关功能的正确性。",
                Capabilities = new List<string> { "workflow", "orchestration", "testing" },
                Tags = new List<string> { "test", "mock", "workflow" },
                InputFormat = "json",
                OutputFormat = "json",
                UsageExample = "var result = await mockAgent.ExecuteWorkflowAsync(config);",
                AgentType = "MockWorkflowAgent",
                InterfaceType = "IMockWorkflowAgent"
            },
            new()
            {
                Id = "MockSocialAgent",
                Name = "模拟社交代理",
                Category = "Social",
                L1Description = "用于测试的模拟社交代理，支持社交平台交互模拟",
                L2Description = "测试环境下的社交代理实现，提供社交平台API的模拟响应，用于测试社交功能而无需真实的外部服务依赖。",
                Capabilities = new List<string> { "social-interaction", "mock-api", "testing" },
                Tags = new List<string> { "test", "mock", "social" },
                InputFormat = "text",
                OutputFormat = "text",
                UsageExample = "var post = await mockAgent.PostAsync('Test message');",
                AgentType = "MockSocialAgent",
                InterfaceType = "IMockSocialAgent"
            }
        };
    }

    /// <summary>
    /// 添加自定义测试Agent
    /// </summary>
    public void AddMockAgent(AgentIndexInfo agent)
    {
        _mockAgents.Add(agent);
    }

    /// <summary>
    /// 移除测试Agent
    /// </summary>
    public void RemoveMockAgent(string agentId)
    {
        _mockAgents.RemoveAll(a => a.Id == agentId);
    }

    /// <summary>
    /// 清除所有Mock Agent
    /// </summary>
    public void ClearMockAgents()
    {
        _mockAgents.Clear();
    }
} 