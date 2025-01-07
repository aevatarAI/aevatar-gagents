using System;
using Orleans;

namespace AevatarGAgents.NamingContest.Common;


[GenerateSerializer]
public class NamingAILogEvent : NamingLogEvent
{
    [Id(0)] public string Request { get; set; }
    
    public NamingAILogEvent(NamingContestStepEnum step, Guid agentId, NamingRoleType roleType = NamingRoleType.None, string agentName = "", string content = "", string request = "") : base(step, agentId, roleType, agentName, content)
    {
        Request = request;
    }
}
