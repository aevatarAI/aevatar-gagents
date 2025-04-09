using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents;
using Aevatar.GAgents.GroupChat.Test;
using Aevatar.AI.Feature.SyncLLMWorker;
using Aevatar.GAgents.AI.BrainFactory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public sealed class AIGAgentTest : AevatarGroupChatTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly Mock<IBrainFactory> _brainFactoryMock;
    private readonly Mock<IBrain> _brainMock;

    public AIGAgentTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();

        _brainFactoryMock = new Mock<IBrainFactory>();
        _brainMock = new Mock<IBrain>();
        
        // ServiceProvider.GetRequiredService<IServiceCollection>()
        //     .AddSingleton(_brainFactoryMock.Object);
    }

    [Fact]
    public async Task AIGAgentChatTest()
    {
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAIGAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a nba player",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        var chatResponse = await chatAIGAgent.ChatAsync("hello");

        chatResponse.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AIGAgentSyncChatTest()
    {
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAIGAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a nba player",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });
        
        await chatAIGAgent.SyncChatAsync("hello");

        await Task.Delay(TimeSpan.FromSeconds(10));
        var state = await chatAIGAgent.GetStateAsync();
        state.IfReceiveMessage.ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeAsync_WithValidConfig_ShouldSucceed()
    {
        // Arrange
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        var initializeDto = new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "OpenAI"
            },
            StreamingModeEnabled = true,
            StreamingConfig = new StreamingConfig
            {
                TimeOutInternal = 10000,
                BufferingSize = 100
            }
        };

        _brainFactoryMock.Setup(x => x.GetBrain(It.IsAny<LLMConfig>()))
            .Returns(_brainMock.Object);

        _brainMock.Setup(x => x.InitializeAsync(It.IsAny<LLMConfig>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await chatAIGAgent.InitializeAsync(initializeDto);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeAsync_WithInvalidConfig_ShouldFail()
    {
        // Arrange
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        var initializeDto = new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "TestSystemLLM"
            }
        };

        _brainFactoryMock.Setup(x => x.GetBrain(It.IsAny<LLMConfig>()))
            .Returns((IBrain)null);

        // Act
        var result = await chatAIGAgent.InitializeAsync(initializeDto);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UploadKnowledgeAsync_WithValidKnowledge_ShouldSucceed()
    {
        // Arrange
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        await chatAIGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "OpenAI"
            }
        });

        var knowledgeList = new List<BrainContentDto>
        {
            new BrainContentDto("test.txt", "This is a test knowledge")
        };

        _brainMock.Setup(x => x.UpsertKnowledgeAsync(It.IsAny<List<BrainContent>>()))
            .Returns(Task.FromResult(true));

        // Act
        var result = await chatAIGAgent.UploadKnowledgeAsync(knowledgeList);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task UploadKnowledgeAsync_WithEmptyKnowledge_ShouldSucceed()
    {
        // Arrange
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        await chatAIGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "OpenAI"
            }
        });

        // Act
        var result = await chatAIGAgent.UploadKnowledgeAsync(null);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task UploadKnowledgeAsync_WithoutInitialization_ShouldFail()
    {
        // Arrange
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        var knowledgeList = new List<BrainContentDto>
        {
            new BrainContentDto("test.txt", "This is a test knowledge")
        };

        // Act
        var result = await chatAIGAgent.UploadKnowledgeAsync(knowledgeList);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleEventAsync_WithValidResponse_ShouldProcessCorrectly()
    {
        // Arrange
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        await chatAIGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test instructions",
            LLMConfig = new LLMConfigDto
            {
                SystemLLM = "OpenAI",
            }
        });

        await chatAIGAgent.ChatAsync("Test message");
        
        // Assert
        var updatedState = await chatAIGAgent.GetStateAsync();
        updatedState.InputTokenUsage.ShouldBeGreaterThan(0);
        updatedState.OutTokenUsage.ShouldBeGreaterThan(0);
        updatedState.TotalTokenUsage.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task HandleEventAsync_WithInvalidRequestId_ShouldNotProcess()
    {
        // Arrange
        var chatAIGAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Assert
        var state = await chatAIGAgent.GetStateAsync();
        state.InputTokenUsage.ShouldBe(0);
        state.OutTokenUsage.ShouldBe(0);
        state.TotalTokenUsage.ShouldBe(0);
    }
}