

using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.JudgeAgent;
using AiSmart.GAgent.NamingContest.VoteAgent;
using Shouldly;
using Xunit;

namespace Aevatar.GAgent.NamingContest.Tests;

public class JudgeGAgentTest: GAgentTestKitBase
{

    [Fact]
    public async Task EventHandlerTrafficNamingContestOverTest()
    {
        var judgeGAgent = await Silo.CreateGrainAsync<JudgeGAgent>(Guid.NewGuid());

        TrafficNamingContestOver trafficNamingContestOver = new TrafficNamingContestOver();
        await judgeGAgent.HandleEventAsync(trafficNamingContestOver);

        JudgeState state = await judgeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe("");
        state.AgentResponsibility.ShouldBe("");
        state.RecentMessages.Count.ShouldBe(1);

    }
    
    [Fact]
    public async Task EventHandlerJudgeVoteGEventTest()
    {
        var judgeGAgent = await Silo.CreateGrainAsync<JudgeGAgent>(Guid.NewGuid());

        JudgeVoteGEvent judgeVoteGEvent = new JudgeVoteGEvent();
        await judgeGAgent.HandleEventAsync(judgeVoteGEvent);

        JudgeState state = await judgeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe("");
        state.AgentResponsibility.ShouldBe("");
        state.RecentMessages.Count.ShouldBe(1);

    }
    
    [Fact]
    public async Task EventHandlerJudgeAskingGEventTest()
    {
        var judgeGAgent = await Silo.CreateGrainAsync<JudgeGAgent>(Guid.NewGuid());

        JudgeAskingGEvent judgeAskingGEvent = new JudgeAskingGEvent();
        await judgeGAgent.HandleEventAsync(judgeAskingGEvent);

        JudgeState state = await judgeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe("");
        state.AgentResponsibility.ShouldBe("");
        state.RecentMessages.Count.ShouldBe(1);

    }
    
    [Fact]
    public async Task EventHandlerJudgeScoreGEventTest()
    {
        var judgeGAgent = await Silo.CreateGrainAsync<JudgeGAgent>(Guid.NewGuid());

        JudgeScoreGEvent judgeScoreGEvent = new JudgeScoreGEvent();
        await judgeGAgent.HandleEventAsync(judgeScoreGEvent);

        JudgeState state = await judgeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe("");
        state.AgentResponsibility.ShouldBe("");
        state.RecentMessages.Count.ShouldBe(1);

    }
    
    
    [Fact]
    public async Task EventHandlerSingleVoteCharmingGEventTest()
    {
        var judgeGAgent = await Silo.CreateGrainAsync<JudgeGAgent>(Guid.NewGuid());

        SingleVoteCharmingGEvent singleVoteCharmingGEvent = new SingleVoteCharmingGEvent();
        await judgeGAgent.HandleEventAsync(singleVoteCharmingGEvent);

        JudgeState state = await judgeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe("");
        state.AgentResponsibility.ShouldBe("");
        state.RecentMessages.Count.ShouldBe(1);

    }
    
}