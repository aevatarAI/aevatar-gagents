using System;
using System.Collections.Generic;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class DtoTest
{
    [Fact]
    public void BrainContentDto_Constructor_WithContentType_ShouldCreateCorrectly()
    {
        // Arrange
        var name = "test.txt";
        var contentType = BrainContentType.String;
        var content = new byte[] { 1, 2, 3, 4, 5 };
        
        // Act
        var dto = new BrainContentDto(name, contentType, content);
        
        // Assert
        dto.Name.ShouldBe(name);
        dto.Type.ShouldBe(contentType);
        dto.Content.ShouldBe(content);
    }
    
    [Fact]
    public void BrainContentDto_Constructor_WithString_ShouldCreateCorrectly()
    {
        // Arrange
        var name = "test.txt";
        var content = "This is a test content";
        
        // Act
        var dto = new BrainContentDto(name, content);
        
        // Assert
        dto.Name.ShouldBe(name);
        dto.Type.ShouldBe(BrainContentType.String);
        dto.Content.ShouldNotBeNull();
        dto.Content.Length.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public void BrainContentDto_ConvertToBrainContent_ShouldConvertCorrectly()
    {
        // Arrange
        var name = "test.txt";
        var contentType = BrainContentType.String;
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var dto = new BrainContentDto(name, contentType, content);
        
        // Act
        var brainContent = dto.ConvertToBrainContent();
        
        // Assert
        brainContent.Name.ShouldBe(name);
        brainContent.Type.ShouldBe(contentType);
        brainContent.Content.ShouldBe(content);
    }
    
    [Fact]
    public void SelfLLMConfig_ConvertToLLMConfig_ShouldConvertCorrectly()
    {
        // Arrange
        var config = new SelfLLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelId = ModelIdEnum.OpenAI,
            ModelName = "gpt-3.5-turbo",
            ApiKey = "test-api-key",
            Endpoint = "https://api.openai.com/v1",
            Memo = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 123 }
            }
        };
        
        // Act
        var llmConfig = config.ConvertToLLMConfig();
        
        // Assert
        llmConfig.ProviderEnum.ShouldBe(config.ProviderEnum);
        llmConfig.ModelIdEnum.ShouldBe(config.ModelId);
        llmConfig.ModelName.ShouldBe(config.ModelName);
        llmConfig.ApiKey.ShouldBe(config.ApiKey);
        llmConfig.Endpoint.ShouldBe(config.Endpoint);
        llmConfig.Memo.ShouldBe(config.Memo);
    }
    
    [Fact]
    public void AIChatContextDto_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var dto = new AIChatContextDto();
        
        // Assert
        dto.RequestId.ShouldBe(Guid.Empty);
        dto.MessageId.ShouldBeNull();
        dto.ChatId.ShouldBeNull();
    }
    
    [Fact]
    public void AIChatContextDto_WithValues_ShouldSetCorrectly()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var messageId = "test-message-id";
        var chatId = "test-chat-id";
        
        // Act
        var dto = new AIChatContextDto
        {
            RequestId = requestId,
            MessageId = messageId,
            ChatId = chatId
        };
        
        // Assert
        dto.RequestId.ShouldBe(requestId);
        dto.MessageId.ShouldBe(messageId);
        dto.ChatId.ShouldBe(chatId);
    }
} 