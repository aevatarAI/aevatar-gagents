using Aevatar.Core.Abstractions;

namespace AiSmart.GAgent.NamingContest.HostAgent.Dto;

public class InitHostDto : InitializationEventBase
{
    public string AgentName { get; set; }
    public string AgentResponsibility { get; set; }
}