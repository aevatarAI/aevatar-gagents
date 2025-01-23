using Aevatar.GAgent.NamingContest.CreativeAgent;
using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.VoteAgent;
using Shouldly;
using Xunit;

namespace Aevatar.GAgent.NamingContest.Tests;

public class CreativGAgentTest: GAgentTestKitBase
{
    [Fact]
    public async Task EventHandlerGroupChatStartGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        var name = "Kobe Bryant";
        var bio =
            "Late basketball legend known for the 'Mamba Mentality' and five NBA championships. Remembered for intense dedication and a relentless pursuit of greatness.";
        var llm = LLMTypesConstant.AzureOpenAI;
        await creativeGAgent.SetAgent(name, bio, llm);
        
        GroupChatStartGEvent groupChatStartGEvent = new GroupChatStartGEvent()
        {
            IfFirstStep = true, 
            ThemeDescribe = "lion"
        };
        await creativeGAgent.HandleEventAsync(groupChatStartGEvent);

        CreativeState state = await creativeGAgent.GetStateAsync();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.AgentResponsibility.ShouldBe(expected: bio);
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerTrafficInformCreativeGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        TrafficInformCreativeGEvent trafficInformCreativeGEvent = new TrafficInformCreativeGEvent();
        await creativeGAgent.HandleEventAsync(trafficInformCreativeGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerSingleVoteCharmingGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        SingleVoteCharmingGEvent singleVoteCharmingGEvent = new SingleVoteCharmingGEvent();
        await creativeGAgent.HandleEventAsync(singleVoteCharmingGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerJudgeAskingCompleteGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        JudgeAskingCompleteGEvent judgeAskingComplete = new JudgeAskingCompleteGEvent();
        await creativeGAgent.HandleEventAsync(judgeAskingComplete);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    
    [Fact]
    public async Task EventHandlerCreativeAnswerQuestionGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        CreativeAnswerQuestionGEvent creativeAnswerQuestion = new CreativeAnswerQuestionGEvent();
        await creativeGAgent.HandleEventAsync(creativeAnswerQuestion);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerCreativeAnswerCompleteGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        CreativeAnswerCompleteGEvent creativeAnswerComplete = new CreativeAnswerCompleteGEvent();
        await creativeGAgent.HandleEventAsync(creativeAnswerComplete);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
  
    
    [Fact]
    public async Task EventHandlerNamedCompleteGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        NamedCompleteGEvent namedCompleteGEvent = new NamedCompleteGEvent();
        await creativeGAgent.HandleEventAsync(namedCompleteGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerDebatedCompleteGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        DebatedCompleteGEvent debatedCompleteGEvent = new DebatedCompleteGEvent();
        await creativeGAgent.HandleEventAsync(debatedCompleteGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerTrafficInformDebateGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        TrafficInformDebateGEvent trafficInformDebateGEvent = new TrafficInformDebateGEvent();
        await creativeGAgent.HandleEventAsync(trafficInformDebateGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
     
    [Fact]
    public async Task EventHandlerDiscussionGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        DiscussionGEvent trafficInformDebateGEvent = new DiscussionGEvent();
        await creativeGAgent.HandleEventAsync(trafficInformDebateGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerDiscussionCompleteGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        DiscussionCompleteGEvent discussionCompleteGEvent = new DiscussionCompleteGEvent();
        await creativeGAgent.HandleEventAsync(discussionCompleteGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerCreativeSummaryGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        CreativeSummaryGEvent creativeSummaryGEvent = new CreativeSummaryGEvent();
        await creativeGAgent.HandleEventAsync(creativeSummaryGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task EventHandlerCreativeSummaryCompleteGEventTest()
    {
        var creativeGAgent = await Silo.CreateGrainAsync<CreativeGAgent>(Guid.NewGuid());

        CreativeSummaryCompleteGEvent creativeSummaryCompleteGEvent = new CreativeSummaryCompleteGEvent();
        await creativeGAgent.HandleEventAsync(creativeSummaryCompleteGEvent);

        CreativeState state = await creativeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        state.RecentMessages.Count.ShouldBe(1);
    }
    
          
}