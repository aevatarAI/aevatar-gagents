

using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.HostAgent;
using AiSmart.GAgent.NamingContest.JudgeAgent;
using AiSmart.GAgent.NamingContest.VoteAgent;
using Shouldly;
using Xunit;

namespace Aevatar.GAgent.NamingContest.Tests;

public class HostGAgentTest: GAgentTestKitBase
{

    [Fact]
    public async Task EventHandlerHostSummaryGEventTest()
    {
        var judgeGAgent = await Silo.CreateGrainAsync<HostGAgent>(Guid.NewGuid());

        HostSummaryGEvent hostSummaryGEvent = new HostSummaryGEvent();
        await judgeGAgent.HandleEventAsync(hostSummaryGEvent);

        HostState state = await judgeGAgent.GetGAgentState();
        
        state.AgentName.ShouldBe("");
        state.AgentResponsibility.ShouldBe("");
        state.RecentMessages.Count.ShouldBe(1);

    }

}