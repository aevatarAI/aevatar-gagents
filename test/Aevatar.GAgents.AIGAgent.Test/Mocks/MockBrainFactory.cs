// ABOUTME: This file implements a mock IBrainFactory for unit testing
// ABOUTME: Creates mock brain instances without real AI service dependencies

using System.Collections.Concurrent;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AIGAgent.Test.Mocks;

public class MockBrainFactory : IBrainFactory
{
    // Cache brain instances by configuration to ensure consistency
    private static readonly ConcurrentDictionary<string, IChatBrain> _chatBrainCache = new();
    private static readonly ConcurrentDictionary<string, ITextToImageBrain> _textToImageBrainCache = new();

    public IBrain? CreateBrain(LLMProviderConfig llmProviderConfig)
    {
        // For simplicity, always return a chat brain as the base implementation
        return GetChatBrain(llmProviderConfig);
    }

    public IChatBrain? GetChatBrain(LLMProviderConfig llmProviderConfig)
    {
        var key = $"{llmProviderConfig.ProviderEnum}_{llmProviderConfig.ModelIdEnum}";
        Console.WriteLine($"[MockBrainFactory] Getting chat brain for key: {key}");
        Console.WriteLine($"[MockBrainFactory] Cache contains {_chatBrainCache.Count} entries");
        
        return _chatBrainCache.GetOrAdd(key, _ =>
        {
            Console.WriteLine($"[MockBrainFactory] Creating new MockChatBrain for key: {key}");
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
        });
    }

    public ITextToImageBrain? GetTextToImageBrain(LLMProviderConfig llmProviderConfig)
    {
        var key = $"{llmProviderConfig.ProviderEnum}_{llmProviderConfig.ModelIdEnum}";
        
        return _textToImageBrainCache.GetOrAdd(key, _ =>
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
        });
    }
    
    // Public method to clear all cached brains (for test cleanup)
    public static void ClearAllCaches()
    {
        _chatBrainCache.Clear();
        _textToImageBrainCache.Clear();
        MockChatBrain.ClearAllSharedState();
        MockTextToImageBrain.ClearAllSharedState();
    }
}