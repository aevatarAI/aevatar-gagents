using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.MCP.Test;

/// <summary>
/// 测试MCP GAgent与AI Agent的集成场景
/// </summary>
public class MCPWithAIGAgentIntegrationTests : AevatarMCPTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public MCPWithAIGAgentIntegrationTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task MCPGAgent_Should_Work_With_Event_Subscription()
    {
        // Arrange - 配置MCP GAgent
        var mcpConfig = new MCPGAgentConfig
        {
            Servers =
            [
                new MCPServerConfig
                {
                    ServerName = "test-integration",
                    Command = "test"
                }
            ]
        };

        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(mcpConfig);

        // 创建一个简单的订阅者GAgent
        var subscriberGAgent = await _gAgentFactory.GetGAgentAsync<ITestSubscriberGAgent>();
        await mcpGAgent.RegisterAsync(subscriberGAgent);

        // Act - 通过订阅者发布工具调用事件
        await subscriberGAgent.CallMCPToolAsync();

        // Assert
        var result = await subscriberGAgent.GetLastResultAsync();
        result.ShouldNotBeNull();
    }
}

public interface ITestSubscriberGAgent : IStateGAgent<TestSubscriberState>
{
    Task CallMCPToolAsync();
    Task<string?> GetLastResultAsync();
}

[GAgent]
public class TestSubscriberGAgent : GAgentBase<TestSubscriberState, TestSubscriberLogEvent>, ITestSubscriberGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test subscriber for MCP events");
    }

    public async Task CallMCPToolAsync()
    {
        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "test-integration",
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object>
            {
                ["param"] = "test"
            }
        };

        await PublishAsync(toolCallEvent);
    }

    [EventHandler]
    public async Task HandleEventAsync(MCPToolResponseEvent @event)
    {
        State.LastResult = @event.Result?.ToString();
        State.LastSuccess = @event.Success;
        await Task.CompletedTask;
    }

    public Task<string?> GetLastResultAsync()
    {
        return Task.FromResult(State.LastResult);
    }
}

[GenerateSerializer]
public class TestSubscriberState : StateBase
{
    [Id(0)] public string? LastResult { get; set; }
    [Id(1)] public bool LastSuccess { get; set; }
}

[GenerateSerializer]
public class TestSubscriberLogEvent : StateLogEventBase<TestSubscriberLogEvent>;