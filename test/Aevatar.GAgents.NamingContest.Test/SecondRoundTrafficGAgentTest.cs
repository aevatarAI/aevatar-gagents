

using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.JudgeAgent;
using AiSmart.GAgent.NamingContest.TrafficAgent;
using Shouldly;
using Xunit;

namespace Aevatar.GAgent.NamingContest.Tests;

public class SecondRoundTrafficGAgentTest: GAgentTestKitBase
{

    [Fact]
    public async Task EventHandlerGroupStartGEventTest()
    {
        var secondRoundTrafficGAgent = await Silo.CreateGrainAsync<SecondRoundTrafficGAgent>(Guid.NewGuid());

        GroupStartGEvent groupStartGEvent = new GroupStartGEvent();
        await secondRoundTrafficGAgent.HandleEventAsync(groupStartGEvent);

        SecondTrafficState state = await secondRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.NamingContent.ShouldBe(groupStartGEvent.Message);
        state.ChatHistory.Count.ShouldBe(1);

    }
    
    
    [Fact]
    public async Task EventHandlerDiscussionCompleteGEventTest()
    {
        var secondRoundTrafficGAgent = await Silo.CreateGrainAsync<SecondRoundTrafficGAgent>(Guid.NewGuid());

        DiscussionCompleteGEvent discussionCompleteGEvent = new DiscussionCompleteGEvent();
        await secondRoundTrafficGAgent.HandleEventAsync(discussionCompleteGEvent);

        SecondTrafficState state = await secondRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerCreativeSummaryCompleteGEventTest()
    {
        var secondRoundTrafficGAgent = await Silo.CreateGrainAsync<SecondRoundTrafficGAgent>(Guid.NewGuid());

        CreativeSummaryCompleteGEvent creativeSummaryCompleteGEvent = new CreativeSummaryCompleteGEvent();
        await secondRoundTrafficGAgent.HandleEventAsync(creativeSummaryCompleteGEvent);

        SecondTrafficState state = await secondRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerJudgeAskingCompleteGEventTest()
    {
        var secondRoundTrafficGAgent = await Silo.CreateGrainAsync<SecondRoundTrafficGAgent>(Guid.NewGuid());

        JudgeAskingCompleteGEvent judgeAskingCompleteGEvent = new JudgeAskingCompleteGEvent();
        await secondRoundTrafficGAgent.HandleEventAsync(judgeAskingCompleteGEvent);

        SecondTrafficState state = await secondRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerCreativeAnswerCompleteGEventTest()
    {
        var secondRoundTrafficGAgent = await Silo.CreateGrainAsync<SecondRoundTrafficGAgent>(Guid.NewGuid());

        CreativeAnswerCompleteGEvent creativeAnswerCompleteGEvent = new CreativeAnswerCompleteGEvent();
        await secondRoundTrafficGAgent.HandleEventAsync(creativeAnswerCompleteGEvent);

        SecondTrafficState state = await secondRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerJudgeScoreCompleteGEventTest()
    {
        var secondRoundTrafficGAgent = await Silo.CreateGrainAsync<SecondRoundTrafficGAgent>(Guid.NewGuid());

        JudgeScoreCompleteGEvent judgeScoreCompleteGEvent = new JudgeScoreCompleteGEvent();
        await secondRoundTrafficGAgent.HandleEventAsync(judgeScoreCompleteGEvent);

        SecondTrafficState state = await secondRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerHostSummaryCompleteGEventTest()
    {
        var secondRoundTrafficGAgent = await Silo.CreateGrainAsync<SecondRoundTrafficGAgent>(Guid.NewGuid());

        HostSummaryCompleteGEvent hostSummaryCompleteGEvent = new HostSummaryCompleteGEvent();
        await secondRoundTrafficGAgent.HandleEventAsync(hostSummaryCompleteGEvent);

        SecondTrafficState state = await secondRoundTrafficGAgent.GetGAgentState();
        
        state.NamingStep.ShouldBe(NamingContestStepEnum.Naming);
        state.ChatHistory.Count.ShouldBe(1);
    }





}