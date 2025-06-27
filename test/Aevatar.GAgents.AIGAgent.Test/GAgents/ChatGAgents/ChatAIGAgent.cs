using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;

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

public class ChatAIGAgent : AIGAgentBase<ChatAIGStateBase, ChatAIStateLogEvent>, IChatAIGAgent
{
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
        return await PromptWithStreamAsync(message, context: contextDto);
    }

    public async Task<bool> PromptChatAsync(string message, AIChatContextDto contextDto)
    {
        return await PromptHttpAsync(message, context: contextDto, ifAsync: false);
    }

    public async Task<List<TextToImageResponse>?> GenerateImageAsync(string prompt,
        TextToImageOption? textToImageOption = null)
    {
        return await base.GenerateImageAsync(prompt, textToImageOption);
    }

    public async Task TextToImageAsync(string prompt, TextToImageOption? textToImageOption = null)
    {
        await base.TextToImageAsync(prompt, new TextToImageContextDto() { Context = Guid.NewGuid().ToString() },
            textToImageOption);
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
                State.ContentList.Add(addMessageLogEvent.Content);
                break;
            case TextToImageLogEvent textToImageLogEvent:
                State.TextToImageResponses = textToImageLogEvent.TextToImageResponses;
                break;
        }
    }
}