using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Orleans;

namespace Aevatar.GAgents.NamingContest.JudgeGAgent;

[GenerateSerializer]
public class JudgeGEvent:EventBase
{
    [Id(0)] public Guid CreativeGrainId { get; set; }
    [Id(1)] public string CreativeName { get; set; }
    [Id(2)] public string NamingReply { get; set; }
    [Id(3)] public string NamingQuestion { get; set; }
}

[GenerateSerializer]
public class JudgeOverGEvent:EventBase
{
    [Id(0)] public string NamingQuestion { get; set; }
}

[GenerateSerializer]
public class JudgeVoteGEVent : EventBase
{
    [Id(0)] public Guid JudgeGrainId { get; set; }
    
    [Id(1)] public List<MicroAIMessage> History { get; set; } = new List<MicroAIMessage>();
}

[GenerateSerializer]
public class JudgeVoteResultGEvent : EventBase
{
    [Id(0)]
    public Guid JudgeGrainId { get; set; }
    [Id(1)]
    public String JudgeName { get; set; }
    [Id(2)]
    public string VoteName { get; set; }
    [Id(3)]
    public string Reason { get; set; }
}