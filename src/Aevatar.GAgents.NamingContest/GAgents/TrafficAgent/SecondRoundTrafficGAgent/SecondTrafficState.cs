using Aevatar.Core.Abstractions;
using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.MicroAI.Model;
using Aevatar.GAgents.NamingContest.Common;

namespace AiSmart.GAgent.NamingContest.TrafficAgent;

public class SecondTrafficState : StateBase
{
    [Id(1)] public List<CreativeInfo> CreativeList { get; set; } = new List<CreativeInfo>();
    [Id(4)] public List<Guid> JudgeAgentList { get; set; } = new List<Guid>();
    [Id(5)] public int AskJudgeCount { get; set; }
    [Id(9)] public string AgentName { get; set; }
    [Id(10)] public string AgentDescription { get; set; }
    [Id(13)] public int Round { get; set; }
    [Id(16)] public int Step { get; set; }
    [Id(14)] public List<Guid> HostAgentList { get; set; } = new List<Guid>();
    [Id(3)] public GrainId HostGroupId { get; set; }
    [Id(14)] public Guid MostCharmingId { get; set; }


    [Id(0)] public List<Guid> CalledGrainIdList { get; set; } = new List<Guid>();
    [Id(2)] public Guid CurrentGrainId { get; set; }
    [Id(3)] public string NamingContent { get; set; }
    [Id(7)] public List<MicroAIMessage> ChatHistory { get; set; } = new List<MicroAIMessage>();
    [Id(8)] public int DiscussionCount { get; set; }
    [Id(12)] public int JudgeScoreCount { get; set; }
    [Id(15)] public NamingContestStepEnum NamingStep { get; set; }

    public void Apply(SecondTrafficSetAgentSEvent @event)
    {
        CreativeList = @event.CreativeList;
        JudgeAgentList = @event.JudgeAgentList;
        HostAgentList = @event.HostAgentList;
        HostGroupId = @event.HostGroupId;
        MostCharmingId = @event.MostCharmingId;
        AgentName = @event.AgentName;
        AgentDescription = @event.AgentDescription;
        AskJudgeCount = @event.AskJudgeCount;
        Round = @event.Round;
        Step = @event.Step;
    }

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

    public void Apply(AddCreativeAgent @event)
    {
        if (CreativeList.Exists(e => e.CreativeGrainId == @event.CreativeGrainId))
        {
            return;
        }

        CreativeList.Add(new CreativeInfo()
            { CreativeName = @event.CreativeName, CreativeGrainId = @event.CreativeGrainId, Naming = @event.Naming });
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

    public void Apply(SetAskingJudgeSEvent @event)
    {
        AskJudgeCount = @event.AskingJudgeCount;
    }

    public void Apply(SetDiscussionSEvent @event)
    {
        DiscussionCount = @event.DiscussionCount;
    }

    public void Apply(DiscussionCountReduceSEvent @event)
    {
        DiscussionCount -= 1;
    }

    public void Apply(AddScoreJudgeCountSEvent @event)
    {
        JudgeScoreCount += 1;
    }

    public void Apply(SetRoundNumberSEvent @event)
    {
        Round = @event.RoundCount;
    }

    public void Apply(ChangeNamingStepSEvent @event)
    {
        NamingStep = @event.Step;
    }

    public void Apply(SetStepNumberSEvent @event)
    {
        Step = @event.StepCount;
    }
}