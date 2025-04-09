using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents;

[GenerateSerializer]
public class ChatAIGStateBase : AIGAgentStateBase
{
    [Id(0)] public bool IfReceiveMessage { get; set; } = false;
}