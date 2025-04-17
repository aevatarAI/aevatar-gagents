using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;

[GenerateSerializer]
public class ChatAIGStateBase : AIGAgentStateBase
{
    [Id(0)] public List<AIStreamChatContent> ContentList = new List<AIStreamChatContent>();
}