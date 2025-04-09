using System;
using System.Collections.Generic;
using Aevatar.AI.Feature.SyncLLMWorker;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.GEvents;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class EventTest
{
    [Fact]
    public void SyncLLMRequestEvent_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var requestEvent = new SyncLLMRequestEvent();
        
        // Assert
        requestEvent.RequestId.ShouldNotBe(Guid.Empty);
        requestEvent.GrainId.ShouldBeNull();
        requestEvent.Instructions.ShouldBeNull();
        requestEvent.IfUseKnowledge.ShouldBeFalse();
        requestEvent.LLMConfig.ShouldBeNull();
        requestEvent.Prompt.ShouldBeNull();
        requestEvent.History.ShouldBeNull();
        requestEvent.PromptSettings.ShouldBeNull();
        requestEvent.ContextDto.ShouldBeNull();
    }
    
    [Fact]
    public void SyncLLMRequestEvent_WithValues_ShouldSetCorrectly()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var grainId = "test-grain-id";
        var instructions = "Test instructions";
        var ifUseKnowledge = true;
        var llmConfig = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI,
            ModelName = "gpt-3.5-turbo",
            ApiKey = "test-api-key",
            Endpoint = "https://api.openai.com/v1"
        };
        var prompt = "Test prompt";
        var history = new List<ChatMessage>
        {
            new ChatMessage(){ChatRole = ChatRole.User, Content = "Hello"}
        };
        var promptSettings = new ExecutionPromptSettings
        {
            Temperature = "0.7",
            MaxToken = 100
        };
        var contextDto = new AIChatContextDto
        {
            RequestId = Guid.NewGuid(),
            MessageId = "test-message-id",
            ChatId = "test-chat-id"
        };
        
        // Act
        var requestEvent = new SyncLLMRequestEvent
        {
            RequestId = requestId,
            GrainId = grainId,
            Instructions = instructions,
            IfUseKnowledge = ifUseKnowledge,
            LLMConfig = llmConfig,
            Prompt = prompt,
            History = history,
            PromptSettings = promptSettings,
            ContextDto = contextDto
        };
        
        // Assert
        requestEvent.RequestId.ShouldBe(requestId);
        requestEvent.GrainId.ShouldBe(grainId);
        requestEvent.Instructions.ShouldBe(instructions);
        requestEvent.IfUseKnowledge.ShouldBe(ifUseKnowledge);
        requestEvent.LLMConfig.ShouldBe(llmConfig);
        requestEvent.Prompt.ShouldBe(prompt);
        requestEvent.History.ShouldBe(history);
        requestEvent.PromptSettings.ShouldBe(promptSettings);
        requestEvent.ContextDto.ShouldBe(contextDto);
    }
    
    [Fact]
    public void SyncLLMResponseEvent_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var responseEvent = new SyncLLMResponseEvent();
        
        // Assert
        responseEvent.RequestId.ShouldBe(Guid.Empty);
        responseEvent.ContextDto.ShouldBeNull();
        responseEvent.ChatResponseList.ShouldBeNull();
        responseEvent.TokenUsageStatistics.ShouldBeNull();
        responseEvent.ErrorMessage.ShouldBeNull();
    }
    
    [Fact]
    public void SyncLLMResponseEvent_WithValues_ShouldSetCorrectly()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var contextDto = new AIChatContextDto
        {
            RequestId = Guid.NewGuid(),
            MessageId = "test-message-id",
            ChatId = "test-chat-id"
        };
        var chatResponseList = new List<ChatMessage>
        {
            new ChatMessage(){ChatRole = ChatRole.Assistant, Content = "Test response"}
        };
        var tokenUsageStatistics = new TokenUsageStatistics
        {
            InputToken = 10,
            OutputToken = 20,
            TotalUsageToken = 30,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var errorMessage = "Test error message";
        
        // Act
        var responseEvent = new SyncLLMResponseEvent
        {
            RequestId = requestId,
            ContextDto = contextDto,
            ChatResponseList = chatResponseList,
            TokenUsageStatistics = tokenUsageStatistics,
            ErrorMessage = errorMessage
        };
        
        // Assert
        responseEvent.RequestId.ShouldBe(requestId);
        responseEvent.ContextDto.ShouldBe(contextDto);
        responseEvent.ChatResponseList.ShouldBe(chatResponseList);
        responseEvent.TokenUsageStatistics.ShouldBe(tokenUsageStatistics);
        responseEvent.ErrorMessage.ShouldBe(errorMessage);
    }
    
    [Fact]
    public void AIStreamingResponseGEvent_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var streamingEvent = new AIStreamingResponseGEvent();
        
        // Assert
        streamingEvent.ResponseContent.ShouldBeNull();
        streamingEvent.SerialNumber.ShouldBe(0);
        streamingEvent.Context.ShouldNotBeNull();
        streamingEvent.IsLastChunk.ShouldBeFalse();
    }
    
    [Fact]
    public void AIStreamingResponseGEvent_WithValues_ShouldSetCorrectly()
    {
        // Arrange
        var responseContent = "Test response content";
        var serialNumber = 1;
        var context = new AIChatContextDto
        {
            RequestId = Guid.NewGuid(),
            MessageId = "test-message-id",
            ChatId = "test-chat-id"
        };
        var isLastChunk = true;
        
        // Act
        var streamingEvent = new AIStreamingResponseGEvent
        {
            ResponseContent = responseContent,
            SerialNumber = serialNumber,
            Context = context,
            IsLastChunk = isLastChunk
        };
        
        // Assert
        streamingEvent.ResponseContent.ShouldBe(responseContent);
        streamingEvent.SerialNumber.ShouldBe(serialNumber);
        streamingEvent.Context.ShouldBe(context);
        streamingEvent.IsLastChunk.ShouldBe(isLastChunk);
    }
} 