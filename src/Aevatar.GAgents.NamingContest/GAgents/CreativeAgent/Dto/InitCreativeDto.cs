using Aevatar.Core.Abstractions;

namespace Aevatar.GAgent.NamingContest.CreativeAgent.Dto;

public class InitCreativeDto:ConfigurationBase
{
    public string AgentName { get; set; }
    public string AgentResponsibility { get; set; }
}