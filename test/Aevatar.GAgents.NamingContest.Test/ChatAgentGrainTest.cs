using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;
using Shouldly;
using Xunit;

namespace Aevatar.GAgent.NamingContest.Tests;

public class ChatAgentGrainTest : GAgentTestKitBase
{
    [Fact]
    public async Task OpenAISendMessageTest()
    {
        var chatAgentGrain = await Silo.CreateGrainAsync<ChatAgentGrain>("ChatAgentGrain");

        var name = "Kobe Bryant";
        var bio =
            "Late basketball legend known for the 'Mamba Mentality' and five NBA championships. Remembered for intense dedication and a relentless pursuit of greatness.";

        await chatAgentGrain.SetAgentAsync(bio,LLMTypesConstant.AzureOpenAI);

        var message = "Create the name of a blockchain project based on personal experience. Just return the project name";
        MicroAIMessage? microAiMessage = await chatAgentGrain.SendAsync(message,null);
        microAiMessage?.Role.ShouldBe("assistant");
        microAiMessage?.Content.ShouldNotBeEmpty();
    }
    [Fact]
    public async Task GoogleGeminiSendMessageTest()
    {
        var chatAgentGrain = await Silo.CreateGrainAsync<ChatAgentGrain>("ChatAgentGrain");

        var name = "Kobe Bryant";
        var bio =
            "Late basketball legend known for the 'Mamba Mentality' and five NBA championships. Remembered for intense dedication and a relentless pursuit of greatness.";

        await chatAgentGrain.SetAgentAsync(bio,LLMTypesConstant.GoogleGemini);

        var message = "Create the name of a blockchain project based on personal experience. Just return the project name";
        MicroAIMessage? microAiMessage = await chatAgentGrain.SendAsync(message,null);
        microAiMessage?.Role.ShouldBe("assistant");
        microAiMessage?.Content.ShouldNotBeEmpty();
    }
}