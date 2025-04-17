using Aevatar.Core.Abstractions;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatWithHistoryGAgent;

public interface IChatWithHistoryGAgent : IChatAgent, IStateGAgent<ChatWithHistoryState>
{
}

[GAgent]
public class ChatWithHistoryGAgent :
    ChatGAgentBase<ChatWithHistoryState, ChatWithHistoryLogEvent, EventBase, ChatConfigDto>, IChatWithHistoryGAgent
{
}