using System.Text.Json.Serialization;

namespace AiSmart.GAgent.NamingContest.JudgeAgent;

public class JudgeVoteInfo
{
    public string AgentName { get; set; }
    public Guid AgentId { get; set; }
    public string Nameing { get; set; }
    public string Reason { get; set; }
}