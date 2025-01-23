using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;
using Shouldly;
using Xunit;

namespace Aevatar.GAgent.NamingContest.Tests;

public class ChatAgentGrainTest : GAgentTestKitBase
{
    [Fact]
    public async Task SendMessageTest()
    {
        var chatAgentGrain = await Silo.CreateGrainAsync<ChatAgentGrain>("ChatAgentGrain");

        var name = "Kobe Bryant";
        var bio =
            "Late basketball legend known for the 'Mamba Mentality' and five NBA championships. Remembered for intense dedication and a relentless pursuit of greatness.";


        await chatAgentGrain.SetAgentAsync(bio);

        var message = "Create a Name Based on Personal Experience";
        MicroAIMessage? microAiMessage = await chatAgentGrain.SendAsync(message,null);
        microAiMessage?.Role.ShouldBe("User");
    }
}