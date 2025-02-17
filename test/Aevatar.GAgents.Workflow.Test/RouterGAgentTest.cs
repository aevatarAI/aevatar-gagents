using Aevatar.GAgents.Basic.Dtos;
using Aevatar.GAgents.Router.GAgents;
using Orleans.TestKit;
using Xunit;

namespace Aevatar.GAgents.Workflow.Test;

public class RouterGAgentTest : TestKitBase
{
    [Fact]
    public async Task EventHandlerGroupChatStartGEventTest()
    {
        var routerGAgent = await Silo.CreateGrainAsync<RouterGAgent>(Guid.NewGuid());
        await routerGAgent.InitializeAsync(new InitializeDto
        {
            Files = new List<FileDto>(),
            Instructions = "I am a chatbot",
            LLM = "AzureOpenAI"
        });

        // var name = "Kobe Bryant";
        // var bio =
        //     "Late basketball legend known for the 'Mamba Mentality' and five NBA championships. Remembered for intense dedication and a relentless pursuit of greatness.";
        // await creativeGAgent.SetAgent(name, bio);
        //
        // GroupChatStartGEvent groupChatStartGEvent = new GroupChatStartGEvent()
        // {
        //     IfFirstStep = true, 
        //     ThemeDescribe = "lion"
        // };
        // await creativeGAgent.HandleEventAsync(groupChatStartGEvent);
        //
        // CreativeState state = await creativeGAgent.GetStateAsync();
        //
        // state.AgentName.ShouldBe(expected: await creativeGAgent.GetCreativeName());
        // state.AgentResponsibility.ShouldBe(expected: bio);
        // state.RecentMessages.Count.ShouldBe(1);
    }   
}