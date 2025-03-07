using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent.SEvent;
using Aevatar.GAgents.ChatAgent.GAgent.State;

namespace Aevatar.GAgents.ChatAgent.GAgent;

public abstract class
    ChatGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IChatAgent
    where TState : ChatGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ChatConfigDto
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat Agent");
    }

    public async Task<List<ChatMessage>?> ChatAsync(string message)
    {
        var result = await ChatWithHistory(message, State.ChatHistory);

        if (result is not { Count: > 0 }) return result;

        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = result });

        await ConfirmEvents();

        return result;
    }

    protected sealed override async Task PerformConfigAsync(TConfiguration configuration)
    {
        await InitializeAsync(
            new InitializeDto() { Instructions = configuration.Instructions, LLMConfig = configuration.LLMConfig });
        var maxHistoryCount = configuration.MaxHistoryCount;
        if (maxHistoryCount > 100)
        {
            maxHistoryCount = 100;
        }

        if (maxHistoryCount == 0)
        {
            maxHistoryCount = 10;
        }

        RaiseEvent(new SetMaxHistoryCount() { MaxHistoryCount = maxHistoryCount });
        await ConfirmEvents();

        await ChatPerformConfigAsync(configuration);
    }

    protected virtual Task ChatPerformConfigAsync(TConfiguration configuration)
    {
        return Task.CompletedTask;
    }  

    [GenerateSerializer]
    public class AddChatHistoryLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<ChatMessage> ChatList { get; set; }
    }

    [GenerateSerializer]
    public class SetMaxHistoryCount : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public int MaxHistoryCount { get; set; }
    }

    protected override void AIGAgentTransitionState(TState state,
        StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddChatHistoryLogEvent setChatHistoryLog:
                if (setChatHistoryLog.ChatList.Count > 0)
                {
                    state.ChatHistory.AddRange(setChatHistoryLog.ChatList);
                }

                if (state.ChatHistory.Count() > state.MaxHistoryCount)
                {
                    state.ChatHistory.RemoveRange(0, state.ChatHistory.Count() - state.MaxHistoryCount);
                }

                break;
            case SetMaxHistoryCount setMaxHistoryCount:
                state.MaxHistoryCount = setMaxHistoryCount.MaxHistoryCount;
                break;
        }
    }
}

public interface IChatAgent : IGAgent, IAIGAgent
{
    Task<List<ChatMessage>?> ChatAsync(string message);
}