// ABOUTME: This file implements a mock ITextToImageBrain for unit testing
// ABOUTME: Provides configurable responses without real AI service calls

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AIGAgent.Test.Mocks;

public class MockTextToImageBrain : ITextToImageBrain
{
    private List<TextToImageResponse>? _nextResponse;

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

    public Task<List<TextToImageResponse>?> GenerateTextToImageAsync(string prompt, TextToImageOption option,
        CancellationToken cancellationToken = default)
    {
        var response = _nextResponse ?? CreateDefaultResponse(option);
        _nextResponse = null; // Reset after use
        return Task.FromResult<List<TextToImageResponse>?>(response);
    }

    public void SetNextResponse(List<TextToImageResponse> response)
    {
        _nextResponse = response;
    }

    private List<TextToImageResponse> CreateDefaultResponse(TextToImageOption option)
    {
        var responses = new List<TextToImageResponse>();
        var count = option.Count > 0 ? option.Count : 1;

        for (int i = 0; i < count; i++)
        {
            var response = new TextToImageResponse
            {
                ResponseType = option.ResponseType,
                ImageType = "png"
            };

            if (option.ResponseType == TextToImageResponseType.Url)
            {
                response.Url = $"https://mock-ai-service.com/image-{i + 1}.png";
                response.Base64Content = "";
            }
            else
            {
                // Small 1x1 transparent PNG as base64
                response.Base64Content = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
                response.Url = "";
            }

            responses.Add(response);
        }

        return responses;
    }
}