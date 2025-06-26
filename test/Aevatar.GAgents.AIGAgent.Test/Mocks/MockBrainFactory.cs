// ABOUTME: This file implements a mock IBrainFactory for unit testing
// ABOUTME: Creates mock brain instances without real AI service dependencies

using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AIGAgent.Test.Mocks;

public class MockBrainFactory : IBrainFactory
{
    public IBrain? CreateBrain(LLMProviderConfig llmProviderConfig)
    {
        // For simplicity, always return a chat brain as the base implementation
        return GetChatBrain(llmProviderConfig);
    }

    public IChatBrain? GetChatBrain(LLMProviderConfig llmProviderConfig)
    {
        var mockBrain = new MockChatBrain();
        
        // Initialize the mock brain with the provider configuration
        var llmConfig = new LLMConfig
        {
            ProviderEnum = llmProviderConfig.ProviderEnum,
            ModelIdEnum = llmProviderConfig.ModelIdEnum
        };
        
        // Synchronously set the configuration (mock doesn't need async initialization)
        mockBrain.InitializeAsync(llmConfig, "mock-brain-id", "Mock brain for testing").Wait();
        
        return mockBrain;
    }

    public ITextToImageBrain? GetTextToImageBrain(LLMProviderConfig llmProviderConfig)
    {
        var mockBrain = new MockTextToImageBrain();
        
        // Initialize the mock brain with the provider configuration
        var llmConfig = new LLMConfig
        {
            ProviderEnum = llmProviderConfig.ProviderEnum,
            ModelIdEnum = llmProviderConfig.ModelIdEnum
        };
        
        // Synchronously set the configuration (mock doesn't need async initialization)
        mockBrain.InitializeAsync(llmConfig, "mock-text-to-image-brain-id", "Mock text-to-image brain for testing").Wait();
        
        return mockBrain;
    }
}