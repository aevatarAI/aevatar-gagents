using System.Threading.RateLimiting;
using Aevatar.GAgent.MicroAI.Tests;
using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.Extensions;
using Orleans.TestKit;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.MicroAi.Tests;

public class ChatAgentGrainTests : GAgentTestKitBase
{
    [Fact]
    public async Task EventHandlerGroupChatStartGEventTest()
    {
        // Load appsettings.json into IConfiguration
        // var configuration = new ConfigurationBuilder()
        //     .SetBasePath(Directory.GetCurrentDirectory())
        //     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        //     .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true)
        //     .Build();
        //
        // Silo.ServiceProvider.AddService(configuration);
        //
        // var configuration2 = Silo.ServiceProvider.GetRequiredService<IConfiguration>();
        //
        // configuration2.Configure<FixedWindowRateLimiterOptions>(configuration2.GetSection("AutogenConfig")); 
        
        var chatAgentGrain = await Silo.CreateGrainAsync<ChatAgentGrain>("ChatAgentGrain");

        var name = "Kobe Bryant";
        var bio =
            "Late basketball legend known for the 'Mamba Mentality' and five NBA championships. Remembered for intense dedication and a relentless pursuit of greatness.";

        
        await chatAgentGrain.SetAgentAsync(bio);

        var count = 10;
        for (var i = 0; i < count; i++)
        {
            string message = "Create a Name Based on Personal Experience";
            MicroAIMessage? microAiMessage = await chatAgentGrain.SendAsync(message,null);
            microAiMessage?.Role.ShouldBe("User");
        }

    }
}