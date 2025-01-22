using System;
using Orleans;

namespace Aevatar.GAgents.NamingContest.Common;


[GenerateSerializer]
public class NamingAiLogGEvent : NamingLogGEvent
{
    [Id(0)] public string Request { get; set; }
    
    public NamingAiLogGEvent(NamingContestStepEnum step, Guid agentId, NamingRoleType roleType = NamingRoleType.None, string agentName = "", string content = "", string request = "") : base(step, agentId, roleType, agentName, content)
    {
        Request = request;
    }
}
