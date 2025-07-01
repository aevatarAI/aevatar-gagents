using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using System;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;

public interface IChatAIGAgent : IAIGAgent, IStateGAgent<ChatAIGStateBase>
{
    Task<string?> ChatAsync(string message);
    Task<bool> StreamChatAsync(string message, AIChatContextDto contextDto);
    Task<bool> PromptChatAsync(string message, AIChatContextDto contextDto);

    Task<List<TextToImageResponse>?> GenerateImageAsync(string prompt,
        TextToImageOption? textToImageOption = null);

    Task TextToImageAsync(string prompt,
        TextToImageOption? textToImageOption = null);
}

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class ChatAIGAgent : AIGAgentBase<ChatAIGStateBase, ChatAIStateLogEvent>, IChatAIGAgent
{
    private IDisposable? _streamTimer;
    private IDisposable? _promptTimer;
    
    public ChatAIGAgent(ILogger<ChatAIGAgent> logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Agent for chatting with user.");
    }

    public async Task<string?> ChatAsync(string message)
    {
        var result = await ChatWithHistory(message);
        return result?[0].Content;
    }

    public async Task<bool> StreamChatAsync(string message, AIChatContextDto contextDto)
    {
        Logger.LogCritical("*** CUSTOM STREAMCHATASYNC CALLED IN TEST IMPLEMENTATION ***");
        
        try
        {
            // Simulate a mock AI response for testing
            string mockResponse = "Mock stream AI response for testing";
            Logger.LogCritical($"*** Using mock stream response: {mockResponse} ***");
            
            // Update state after delay to match test expectations
            _ = Task.Run(async () =>
            {
                await Task.Delay(50); // Very short delay for async simulation
                try
                {
                    Logger.LogCritical("*** STREAM DELAYED UPDATE EXECUTING ***");
                    await AIChatHandleStreamAsync(contextDto, AIExceptionEnum.None, null, 
                        new AIStreamChatContent()
                        {
                            ResponseContent = mockResponse
                        });
                    Logger.LogCritical("*** STREAM STATE UPDATED SUCCESSFULLY ***");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Delayed stream state update failed: {ex}");
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"StreamChatAsync failed: {ex}");
            return false;
        }
    }

    public async Task<bool> PromptChatAsync(string message, AIChatContextDto contextDto)
    {
        Logger.LogCritical("*** CUSTOM PROMPTCHATASYNC CALLED IN TEST IMPLEMENTATION ***");
        
        try
        {
            // Simulate a mock AI response for testing
            string mockResponse = "Mock AI response";
            Logger.LogCritical($"*** Using mock response: {mockResponse} ***");
            
            // Update state after delay to match test expectations
            _ = Task.Run(async () =>
            {
                await Task.Delay(50); // Very short delay for async simulation
                try
                {
                    Logger.LogCritical("*** DELAYED UPDATE EXECUTING ***");
                    await AIChatHttpResponseHandleAsync(contextDto, AIExceptionEnum.None, null, mockResponse);
                    Logger.LogCritical("*** STATE UPDATED SUCCESSFULLY ***");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Delayed state update failed: {ex}");
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"PromptChatAsync failed: {ex}");
            return false;
        }
    }

    public async Task<List<TextToImageResponse>?> GenerateImageAsync(string prompt,
        TextToImageOption? textToImageOption = null)
    {
        return await base.GenerateImageAsync(prompt, textToImageOption);
    }

    public async Task TextToImageAsync(string prompt, TextToImageOption? textToImageOption = null)
    {
        try
        {
            // For testing, simulate the async worker behavior synchronously
            var context = new TextToImageContextDto() { Context = Guid.NewGuid().ToString() };
            var result = await base.GenerateImageAsync(prompt, textToImageOption);
            if (result != null && result.Count > 0)
            {
                // Simulate the async worker response handling
                await AITextToImageHandleAsync(context, AIExceptionEnum.None, null, result);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"TextToImageAsync failed: {ex.Message}");
        }
    }

    [EventHandler]
    public async Task OnChatAIEvent(ChatEvent @event)
    {
        var result = await ChatAsync(@event.Message);
        Logger.LogInformation("Chat output: {Result}", result);
    }

    protected override async Task AIChatHandleStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        AIStreamChatContent? content)
    {
        if (content != null)
        {
            RaiseEvent(new AddMessageLogEvent()
            {
                Content = content
            });

            await ConfirmEvents();
        }
    }

    protected override async Task AIChatHttpResponseHandleAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        string? content)
    {
        if (content != null)
        {
            RaiseEvent(new AddMessageLogEvent()
            {
                Content = new AIStreamChatContent()
                {
                    ResponseContent = content
                }
            });

            await ConfirmEvents();
        }

        Logger.LogInformation("[ChatAIGAgent][AIChatHttpResponseHandleAsync] has done");
    }

    protected override async Task AITextToImageHandleAsync(TextToImageContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage, List<TextToImageResponse>? imageResponses)
    {
        if (imageResponses != null && imageResponses.Count > 0)
        {
            RaiseEvent(new TextToImageLogEvent() { TextToImageResponses = imageResponses });
            await ConfirmEvents();
        }
    }

    protected override void AIGAgentTransitionState(ChatAIGStateBase state,
        StateLogEventBase<ChatAIStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddMessageLogEvent addMessageLogEvent:
                state.ContentList.Add(addMessageLogEvent.Content);
                break;
            case TextToImageLogEvent textToImageLogEvent:
                state.TextToImageResponses = textToImageLogEvent.TextToImageResponses;
                break;
        }
    }
}