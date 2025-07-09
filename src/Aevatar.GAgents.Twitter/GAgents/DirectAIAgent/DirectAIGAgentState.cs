using System;
using Aevatar.GAgents.AIGAgent.State;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents.DirectAIAgent;

[GenerateSerializer]
public class DirectAIGAgentState : AIGAgentStateBase
{
    [Id(0)]
    public string LastResponse { get; set; } = "";
    
    [Id(1)]
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
    
    [Id(2)]
    public int TotalInteractions { get; set; } = 0;
} 