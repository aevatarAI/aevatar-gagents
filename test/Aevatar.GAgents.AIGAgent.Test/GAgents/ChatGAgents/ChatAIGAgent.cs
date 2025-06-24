using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;

public interface IChatAIGAgent : IAIGAgent, IStateGAgent<ChatAIGStateBase>
{
    Task<string?> ChatAsync(string message, List<string>? images = null);
    Task<bool> StreamChatAsync(string message, AIChatContextDto contextDto, List<string>? images = null);
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

    public async Task<string?> ChatAsync(string message, List<string>? images = null)
    {
        var result = await ChatWithHistory(message, imageKeys: images);
        return result?[0].Content;
    }

    public async Task<bool> StreamChatAsync(string message, AIChatContextDto contextDto, List<string>? images = null)
    {
        return await PromptWithStreamAsync(message, context: contextDto, imageKeys: images);
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

    protected override void AIGAgentTransitionState(ChatAIGStateBase state,
        StateLogEventBase<ChatAIStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddMessageLogEvent addMessageLogEvent:
                State.ContentList.Add(addMessageLogEvent.Content);
                break;
        }
    }
}