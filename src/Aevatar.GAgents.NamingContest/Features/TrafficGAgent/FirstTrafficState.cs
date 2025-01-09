using System;
using System.Collections.Generic;
using System.Linq;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.NamingContest.Common;
using Orleans;

namespace Aevatar.GAgents.NamingContest.TrafficGAgent;

public class FirstTrafficState : StateBase
{
    [Id(0)] public List<Guid> CalledGrainIdList { get; set; } = new List<Guid>();
    [Id(1)] public List<CreativeInfo> CreativeList { get; set; } = new List<CreativeInfo>();
    [Id(2)] public Guid CurrentGrainId { get; set; }
    [Id(3)] public string NamingContent { get; set; }
    [Id(4)] public string AgentName { get; set; }
    [Id(5)] public string Description { get; set; }
    [Id(6)] public int DebateRoundCount { get; set; }
    [Id(7)] public List<Guid> DebateList { get; set; }
    [Id(8)] public NamingContestStepEnum NamingStep { get; set; }

    [Id(9)] public List<Guid> JudgeAgentList { get; set; } = new List<Guid>();
    [Id(10)] public List<MicroAIMessage> ChatHistory { get; set; } = new List<MicroAIMessage>();
    
    [Id(11)] public List<Guid> HostAgentList { get; set; } = new List<Guid>();

    public void Apply(TrafficCallSelectGrainIdSEvent sEvent)
    {
        CurrentGrainId = sEvent.GrainId;
    }

    public void Apply(TrafficNameStartSEvent @event)
    {
        NamingContent = @event.Content;
    }

    public void Apply(TrafficGrainCompleteSEvent sEvent)
    {
        CalledGrainIdList.Add(sEvent.CompleteGrainId);
        CurrentGrainId = Guid.Empty;
    }

    public void Apply(TrafficSetAgentSEvent @event)
    {
        AgentName = @event.AgentName;
        Description = @event.Description;
    }

    public void Apply(AddCreativeAgent @event)
    {
        if (CreativeList.Exists(e => e.CreativeGrainId == @event.CreativeGrainId))
        {
            return;
        }

        CreativeList.Add(new CreativeInfo()
            { CreativeName = @event.CreativeName, CreativeGrainId = @event.CreativeGrainId });
    }

    public void Apply(ChangeNamingStepSEvent @event)
    {
        NamingStep = @event.Step;
    }

    public void Apply(SetDebateCountSEvent @event)
    {
        DebateRoundCount = @event.DebateCount;
    }

    public void Apply(ReduceDebateRoundSEvent @event)
    {
        DebateRoundCount -= 1;
    }

    public void Apply(AddChatHistorySEvent @event)
    {
        ChatHistory.Add(@event.ChatMessage);
    }

    public void Apply(AddJudgeSEvent @event)
    {
        this.JudgeAgentList.Add(@event.JudgeGrainId);
    }
    
    public void Apply(AddHostSEvent @event)
    {
        this.HostAgentList.Add(@event.HostGrainId);
    }

    public void Apply(ClearCalledGrainsSEvent @event)
    {
        this.CalledGrainIdList.Clear();
    }

    public void Apply(CreativeNamingSEvent @event)
    {
        var creativeInfo = this.CreativeList.FirstOrDefault(f => f.CreativeGrainId == @event.CreativeId);
        if (creativeInfo != null)
        {
            creativeInfo.Naming = @event.Naming;
        }
    }
}

[GenerateSerializer]
public class CreativeInfo
{
    [Id(0)] public string CreativeName { get; set; }
    [Id(1)] public Guid CreativeGrainId { get; set; }
    [Id(2)] public string Naming { get; set; }

}