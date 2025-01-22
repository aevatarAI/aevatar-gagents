

using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.JudgeAgent;
using Shouldly;
using Xunit;

namespace Aevatar.GAgent.NamingContest.Tests;

public class FirstRoundTrafficGAgentTest: GAgentTestKitBase
{

    [Fact]
    public async Task EventHandlerGroupStartGEventTest()
    {
        var firstRoundTrafficGAgent = await Silo.CreateGrainAsync<FirstRoundTrafficGAgent>(Guid.NewGuid());

        GroupStartGEvent groupStartGEvent = new GroupStartGEvent();
        await firstRoundTrafficGAgent.HandleEventAsync(groupStartGEvent);

        FirstTrafficState state = await firstRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.NamingContent.ShouldBe(groupStartGEvent.Message);
        state.ChatHistory.Count.ShouldBe(1);

    }
    
    [Fact]
    public async Task EventHandlerNamedCompleteGEventTest()
    {
        var firstRoundTrafficGAgent = await Silo.CreateGrainAsync<FirstRoundTrafficGAgent>(Guid.NewGuid());

        NamedCompleteGEvent namedCompleteGEvent = new NamedCompleteGEvent();
        await firstRoundTrafficGAgent.HandleEventAsync(namedCompleteGEvent);

        FirstTrafficState state = await firstRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);

    }
    
    [Fact]
    public async Task EventHandlerDebatedCompleteGEventTest()
    {
        var firstRoundTrafficGAgent = await Silo.CreateGrainAsync<FirstRoundTrafficGAgent>(Guid.NewGuid());

        DebatedCompleteGEvent debatedCompleteGEvent = new DebatedCompleteGEvent();
        await firstRoundTrafficGAgent.HandleEventAsync(debatedCompleteGEvent);

        FirstTrafficState state = await firstRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);

    }
    
    [Fact]
    public async Task EventHandlerJudgeVoteResultGEventTest()
    {
        var firstRoundTrafficGAgent = await Silo.CreateGrainAsync<FirstRoundTrafficGAgent>(Guid.NewGuid());

        JudgeVoteResultGEvent judgeVoteResultGEvent = new JudgeVoteResultGEvent();
        await firstRoundTrafficGAgent.HandleEventAsync(judgeVoteResultGEvent);

        FirstTrafficState state = await firstRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);

    }
    
    [Fact]
    public async Task EventHandlerHostSummaryCompleteGEventTest()
    {
        var firstRoundTrafficGAgent = await Silo.CreateGrainAsync<FirstRoundTrafficGAgent>(Guid.NewGuid());

        HostSummaryCompleteGEvent hostSummaryCompleteGEvent = new HostSummaryCompleteGEvent();
        await firstRoundTrafficGAgent.HandleEventAsync(hostSummaryCompleteGEvent);

        FirstTrafficState state = await firstRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);

    }
    
       
}