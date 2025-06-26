// ABOUTME: This file implements a mock IChatBrain for unit testing
// ABOUTME: Provides configurable responses without real AI service calls

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AIGAgent.Test.Mocks;

public class MockChatBrain : IChatBrain
{
    private InvokePromptResponse? _nextResponse;
    private readonly Queue<string> _streamingResponses = new();

    public LLMProviderEnum ProviderEnum { get; private set; }
    public ModelIdEnum ModelIdEnum { get; private set; }

    public Task InitializeAsync(LLMConfig llmConfig, string id, string description)
    {
        ProviderEnum = llmConfig.ProviderEnum;
        ModelIdEnum = llmConfig.ModelIdEnum;
        return Task.CompletedTask;
    }

    public Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null)
    {
        return Task.FromResult(true);
    }

    public Task<InvokePromptResponse?> InvokePromptAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default)
    {
        var response = _nextResponse ?? CreateDefaultResponse();
        _nextResponse = null; // Reset after use
        return Task.FromResult<InvokePromptResponse?>(response);
    }

    public Task<IAsyncEnumerable<object>> InvokePromptStreamingAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default)
    {
        var responses = _streamingResponses.Count > 0 
            ? _streamingResponses.ToArray() 
            : new[] { "Mock", " streaming", " response" };

        return Task.FromResult(CreateStreamingResponse(responses));
    }

    public TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList)
    {
        return new TokenUsageStatistics
        {
            InputToken = 5,
            OutputToken = messageList.Count * 2,
            TotalUsageToken = 5 + (messageList.Count * 2),
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    public void SetNextResponse(InvokePromptResponse response)
    {
        _nextResponse = response;
    }

    public void SetStreamingResponses(params string[] responses)
    {
        _streamingResponses.Clear();
        foreach (var response in responses)
        {
            _streamingResponses.Enqueue(response);
        }
    }

    private InvokePromptResponse CreateDefaultResponse()
    {
        return new InvokePromptResponse
        {
            ChatReponseList = new List<ChatMessage>
            {
                new() { ChatRole = ChatRole.Assistant, Content = "Mock AI response" }
            },
            TokenUsageStatistics = new TokenUsageStatistics
            {
                InputToken = 10,
                OutputToken = 15,
                TotalUsageToken = 25,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };
    }

    private static async IAsyncEnumerable<object> CreateStreamingResponse(string[] responses)
    {
        foreach (var response in responses)
        {
            yield return response;
            await Task.Delay(10); // Simulate streaming delay
        }
    }
}