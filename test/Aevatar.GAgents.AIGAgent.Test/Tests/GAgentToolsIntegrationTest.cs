using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Integration tests for GAgent tools functionality
/// </summary>
public sealed class GAgentToolsIntegrationTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly IGAgentService _gAgentService;
    private readonly IGAgentExecutor _gAgentExecutor;

    public GAgentToolsIntegrationTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentService = GetRequiredService<IGAgentService>();
        _gAgentExecutor = GetRequiredService<IGAgentExecutor>();
    }

    [Fact]
    public async Task Should_Execute_Complete_GAgent_Tools_Workflow()
    {
        // Arrange - Create and initialize AI agent with tools
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are an AI assistant that can coordinate with other agents",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
            EnableGAgentTools = true
        });

        // Act - Verify tools are enabled
        var state = await chatAgent.GetStateAsync();
        
        // Assert - Basic configuration
        state.EnableGAgentTools.ShouldBeTrue();
        state.SystemLLM.ShouldBe("OpenAI");
        
        // Verify GAgentService is working
        var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();
        allGAgents.ShouldNotBeNull();
        allGAgents.Count.ShouldBeGreaterThan(0);
        
        // Verify we found some GAgents
        allGAgents.Keys.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Filter_GAgents_Based_On_Allowed_Types()
    {
        // Arrange
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        var allowedTypes = new List<GrainType> { chatAgent.GetGrainId().Type };
        
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are an AI with limited agent access",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
            EnableGAgentTools = true,
            AllowedGAgentTypes = allowedTypes
        });
        
        // Act
        var state = await chatAgent.GetStateAsync();
        
        // Assert
        state.EnableGAgentTools.ShouldBeTrue();
        state.AllowedGAgentTypes.ShouldNotBeNull();
        state.AllowedGAgentTypes.Count.ShouldBe(1);
        state.AllowedGAgentTypes[0].ToString()!.ShouldContain("ChatAIGAgent");
    }

    [Fact]
    public async Task Should_Handle_Multiple_Agents_With_Tools()
    {
        // Test multiple agents can be created with tools enabled
        
        // Arrange & Act
        var agent1 = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await agent1.InitializeAsync(new InitializeDto
        {
            Instructions = "First AI assistant",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
            EnableGAgentTools = true
        });
        
        var agent2 = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await agent2.InitializeAsync(new InitializeDto
        {
            Instructions = "Second AI assistant",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
            EnableGAgentTools = true,
            AllowedGAgentTypes = [GrainType.Create("GroupGAgent")]
        });
        
        // Assert
        var state1 = await agent1.GetStateAsync();
        var state2 = await agent2.GetStateAsync();
        
        state1.EnableGAgentTools.ShouldBeTrue();
        state2.EnableGAgentTools.ShouldBeTrue();
        
        // Agent 1 has no restrictions
        state1.AllowedGAgentTypes.ShouldBeEmpty();
        
        // Agent 2 has restrictions
        state2.AllowedGAgentTypes.ShouldNotBeNull();
        state2.AllowedGAgentTypes.Count.ShouldBe(1);
        state2.AllowedGAgentTypes[0].ToString().ShouldBe("GroupGAgent");
    }

    [Fact]
    public async Task Should_Maintain_State_After_Tools_Registration()
    {
        // Verify state is properly maintained after enabling tools
        
        // Arrange
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        // Act - Initialize with tools
        await chatAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a helpful AI assistant",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
            EnableGAgentTools = true
        });
        
        // Add some chat history
        var chatResult = await chatAgent.ChatAsync("Hello!");
        
        // Assert - Verify state integrity
        var state = await chatAgent.GetStateAsync();
        state.EnableGAgentTools.ShouldBeTrue();
        state.SystemLLM.ShouldBe("OpenAI");
        state.PromptTemplate.ShouldBe("You are a helpful AI assistant");
        
        // Chat history should be maintained
        state.ChatHistory.Count.ShouldBeGreaterThan(0);
    }



    [Fact]
    public async Task Should_Handle_Concurrent_Tool_Registration()
    {
        // Test concurrent agent creation with tools
        
        // Arrange & Act
        var tasks = new List<Task<IChatAIGAgent>>();
        
        for (int i = 0; i < 5; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
                await agent.InitializeAsync(new InitializeDto
                {
                    Instructions = $"AI Assistant #{index}",
                    LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
                    EnableGAgentTools = index % 2 == 0 // Even indices have tools enabled
                });
                return agent;
            }));
        }
        
        var agents = await Task.WhenAll(tasks);
        
        // Assert
        agents.Length.ShouldBe(5);
        
        for (int i = 0; i < agents.Length; i++)
        {
            var state = await agents[i].GetStateAsync();
            state.PromptTemplate.ShouldBe($"AI Assistant #{i}");
            state.EnableGAgentTools.ShouldBe(i % 2 == 0);
        }
    }
} 