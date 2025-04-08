using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using SimpleAIGAgent.Grains.Agents.Events;

namespace SimpleAIGAgent.Grains.Agents.Chat;

public interface IChatAIGAgent : IAIGAgent, IGAgent
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
        Console.WriteLine($"chatResponseList:{chatResponseList}");
    }
    
}