using Aevatar.Core.Abstractions;

namespace AiSmart.GAgent.NamingContest.HostAgent.Dto;

public class InitHostDto : ConfigurationBase
{
    public string AgentName { get; set; }
    public string AgentResponsibility { get; set; }
}