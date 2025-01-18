

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Model;
using Aevatar.GAgents.NamingContest.Common;

namespace Aevatar.GAgent.NamingContest.TrafficGAgent;

public class FirstTrafficState : StateBase
{
    [Id(0)] public List<CreativeInfo> CreativeList { get; set; } = new List<CreativeInfo>();
    [Id(1)] public List<Guid> JudgeAgentList { get; set; } = new List<Guid>();
    [Id(2)] public List<Guid> HostAgentList { get; set; } = new List<Guid>();
    [Id(3)] public Guid HostGroupId { get; set; }
    [Id(4)] public int Step { get; set; }
    [Id(5)] public Guid MostCharmingId { get; set; }
    
    [Id(6)] public List<Guid> CalledGrainIdList { get; set; } = new List<Guid>();
    [Id(7)] public Guid CurrentGrainId { get; set; }
    [Id(8)] public string NamingContent { get; set; }
    [Id(9)] public int DebateRoundCount { get; set; }
    [Id(10)] public NamingContestStepEnum NamingStep { get; set; }
    [Id(11)] public List<MicroAIMessage> ChatHistory { get; set; } = new List<MicroAIMessage>();
    
    public void Apply(FirstTrafficSetAgentSEvent @event)
    {
        CreativeList = @event.CreativeList;
        JudgeAgentList = @event.JudgeAgentList;
        HostAgentList = @event.HostAgentList;
        HostGroupId = @event.HostGroupId;
        Step = @event.Step;
        MostCharmingId = @event.MostCharmingId;
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

    public void Apply(SetStepNumberSEvent @event)
    {
        Step = @event.StepCount;
    }
}

[GenerateSerializer]
public class CreativeInfo
{
    [Id(0)] public string CreativeName { get; set; }
    [Id(1)] public Guid CreativeGrainId { get; set; }
    [Id(2)] public string Naming { get; set; }

}