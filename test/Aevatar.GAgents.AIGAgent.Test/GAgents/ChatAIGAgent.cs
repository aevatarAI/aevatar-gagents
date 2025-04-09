using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents;

public interface IChatAIGAgent : IAIGAgent, IStateGAgent<ChatAIGStateBase>
{
    Task<string?> ChatAsync(string message);

    Task SyncChatAsync(string message);
}

[GAgent]
public class ChatAigAgent : AIGAgentBase<ChatAIGStateBase, ChatAIStateLogEvent>, IChatAIGAgent
{
    public ChatAigAgent(ILogger<ChatAigAgent> logger)
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

    public async Task SyncChatAsync(string message)
    {
        await SyncChatWithHistoryAsync(message);
    }

    [EventHandler]
    public async Task OnChatAIEvent(ChatEvent @event)
    {
        var result = await ChatAsync(@event.Message);
        Logger.LogInformation("Chat output: {Result}", result);
    }

    protected override async Task SyncLLMResponseHandlerAsync(List<ChatMessage>? chatResponseList, string errorMessage,
        AIChatContextDto? context = null)
    {
        RaiseEvent(new ReceiveMessageLogEvent { ReceiveMessage = true });
        await ConfirmEvents();
        
        Console.WriteLine($"chatResponseList:{chatResponseList}");
    }

    protected override void AIGAgentTransitionState(ChatAIGStateBase state, StateLogEventBase<ChatAIStateLogEvent> @event)
    {
        switch (@event)
        {
            case ReceiveMessageLogEvent receiveMessageLogEvent:
                State.IfReceiveMessage = receiveMessageLogEvent.ReceiveMessage;
                break;
        }
    }
}