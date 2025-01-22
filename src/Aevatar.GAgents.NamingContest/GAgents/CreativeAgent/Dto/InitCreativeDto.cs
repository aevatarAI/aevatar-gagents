using Aevatar.Core.Abstractions;

namespace Aevatar.GAgent.NamingContest.CreativeAgent.Dto;

public class InitCreativeDto:InitializationEventBase
{
    public string AgentName { get; set; }
    public string AgentResponsibility { get; set; }
    
    public string Llm { get; set; }
}