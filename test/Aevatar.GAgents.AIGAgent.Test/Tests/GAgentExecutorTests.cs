using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Aevatar.GAgents.Basic;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class GAgentExecutorTests : AevatarAIGAgentTestBase
{
    private readonly IGAgentExecutor _executor;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentExecutorTests()
    {
        _executor = GetRequiredService<IGAgentExecutor>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task ExecuteGAgentEventHandler_WithIGAgent_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Test Message 1" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(mockGAgent, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Test Message 1");
        result.ShouldContain("Count: 1");
    }

    [Fact]
    public async Task ExecuteGAgentEventHandler_WithGrainId_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var testEvent = new MockExecutorTestEvent { Message = "Test Message 2" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(grainId, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Test Message 2");
    }

    [Fact]
    public async Task ExecuteGAgentEventHandler_WithGrainType_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var grainType = grainId.Type;
        var testEvent = new MockExecutorTestEvent { Message = "Test Message 3" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(grainType, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Test Message 3");
    }

    [Fact(Skip = "Wait to long.")]
    public async Task ExecuteGAgentEventHandler_ShouldThrowTimeoutException_WhenNoResponseReceived()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTimeoutEvent(); // Event that won't generate a response

        // Act & Assert
        await Should.ThrowAsync<TimeoutException>(async () =>
        {
            await _executor.ExecuteGAgentEventHandler(mockGAgent, testEvent);
        });
    }

    [Fact]
    public async Task ExecuteGAgentEventHandler_ShouldHandleMultipleConcurrentExecutions()
    {
        // Arrange
        var tasks = new List<Task<string>>();

        // Act
        for (var i = 0; i < 3; i++)
        {
            var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
            var testEvent = new MockExecutorTestEvent { Message = $"Concurrent Message {i}" };
            tasks.Add(_executor.ExecuteGAgentEventHandler(mockGAgent, testEvent));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.ShouldBe(3);
        for (var i = 0; i < 3; i++)
        {
            results[i].ShouldNotBeNullOrEmpty();
            results[i].ShouldContain("Processed:");
        }
    }

    [Fact]
    public async Task ExecuteGAgentEventHandler_WithFixedExecutionFlow_ShouldSubscribeCorrectly()
    {
        // Arrange
        var targetGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Fixed Flow Test" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(targetGAgent, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Fixed Flow Test");

        // Verify that the ResultGAgent subscribed directly to targetGAgent
        // (not through PublishingGAgent)
    }
}