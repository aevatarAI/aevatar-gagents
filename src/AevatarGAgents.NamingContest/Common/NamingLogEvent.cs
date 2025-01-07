using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.NamingContest.Common;

[GenerateSerializer]
public class NamingLogEvent : EventBase
{
    [Id(0)] public Guid EventId = Guid.NewGuid();
    [Id(1)] public NamingContestStepEnum Step { get; set; }
    [Id(2)] public NamingRoleType RoleType { get; set; }
    [Id(3)] public Guid AgentId { get; set; } = Guid.Empty;
    [Id(4)] public string AgentName { get; set; }
    [Id(5)] public string Content { get; set; }
    [Id(6)] public long Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public NamingLogEvent(NamingContestStepEnum step, Guid agentId, NamingRoleType roleType = NamingRoleType.None,
        string agentName = "", string content = "")
    {
        Step = step;
        AgentId = agentId;
        RoleType = roleType;
        AgentName = agentName;
        Content = content;
    }
}

[GenerateSerializer]
public enum NamingContestStepEnum
{
    NamingStart = 0,
    Naming = 1,
    DebateStart = 2,
    Debate = 3,
    DiscussionStart = 4,
    Discussion = 5,
    DiscussionSummary = 6,
    JudgeVoteStart = 7,
    JudgeVote = 8,
    JudgeStartScore = 9,
    JudgeScore = 10,
    JudgeStartAsking = 11,
    JudgeAsking = 12,
    Complete = 13,
    HosSummaryStart = 14,
    HostSummary = 15,
    HostSummaryComplete = 16,
}

[GenerateSerializer]
public enum NamingRoleType
{
    None = 0,
    Contestant = 1,
    Judge = 2,
    Host = 3,
}