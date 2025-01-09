using System;
using System.Text.Json.Serialization;

namespace Aevatar.GAgents.NamingContest.JudgeGAgent;

public class JudgeVoteInfo
{
    public string AgentName { get; set; }
    public Guid AgentId { get; set; }
    public string Nameing { get; set; }
    public string Reason { get; set; }
}

public class JudgeVoteChatResponse
{
    [JsonPropertyName(@"name")] public string Name { get; set; }

    [JsonPropertyName(@"reason")] public string Reason { get; set; }
}