using Aevatar.Core.Abstractions;
using Aevatar.GAgent.NamingContest.CreativeAgent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.Basic.PublishGAgent;
using Aevatar.GAgents.Router.Demo.GAgents;
using Aevatar.GAgents.Router.GAgents;
using Aevatar.GAgents.Router.GEvents;
using AiSmart.GAgent.NamingContest.HostAgent;
using AiSmart.GAgent.NamingContest.VoteAgent;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Workflow.Test;

public class RouterGAgentTest : AevatarWorkflowTestBase
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentManager _gAgentManager;
    private readonly GrainTypeResolver _grainTypeResolver;

    public RouterGAgentTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentManager = GetRequiredService<IGAgentManager>();
        var clusterClient = GetRequiredService<IClusterClient>();
        _grainTypeResolver = clusterClient.ServiceProvider.GetRequiredService<GrainTypeResolver>();
    }
    
    [Fact]
    public async Task EventHandlerGroupChatStartGEventTest()
    {
        var routerGAgent = await _gAgentFactory.GetGAgentAsync<IRouterGAgent>(Guid.NewGuid());
        await routerGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "I am a chatbot",
            LLM = "AzureOpenAI"
        });

        // var ans = await routerGAgent.InvokeLLMAsync("what is 1 + 1?");
        
        var hostGAgent = await _gAgentFactory.GetGAgentAsync(Guid.NewGuid(), typeof(HostGAgent));
        await hostGAgent.RegisterAsync(routerGAgent);
        
        var creativeGAgent = await _gAgentFactory.GetGAgentAsync<ICreativeGAgent>(Guid.NewGuid());
        await hostGAgent.RegisterAsync(creativeGAgent);
        var events = await creativeGAgent.GetAllSubscribedEventsAsync();
        await routerGAgent.AddAgentDescription(creativeGAgent.GetType(), events);
        
        var voteGAgent = await _gAgentFactory.GetGAgentAsync<IVoteCharmingGAgent>(Guid.NewGuid());
        await hostGAgent.RegisterAsync(voteGAgent);
        events = await voteGAgent.GetAllSubscribedEventsAsync();
        await routerGAgent.AddAgentDescription(voteGAgent.GetType(), events);
        
        var publishGAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>(Guid.NewGuid());
        await publishGAgent.RegisterAsync(hostGAgent);
        await publishGAgent.PublishEventAsync(new RequestAllSubscriptionsEvent()
        {
        });
        
        await Task.Delay(8000);
        
        var state = await routerGAgent.GetStateAsync();
        var children = await hostGAgent.GetChildrenAsync();
        Assert.Equal(3, children.Count);
    }   
    
    [Fact]
    public async Task RouterTest()
    {
        var routerGAgent = await _gAgentFactory.GetGAgentAsync<IRouterGAgent>(Guid.NewGuid());
        await routerGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a router agent",
            LLM = "AzureOpenAI"
        });
        
        
        var blockChainGAgent = await _gAgentFactory.GetGAgentAsync<IBlockChainGAgent>(Guid.NewGuid());
        var blockChainGAgentEvents = await blockChainGAgent.GetAllSubscribedEventsAsync();
        await routerGAgent.AddAgentDescription(blockChainGAgent.GetType(), blockChainGAgentEvents);
        
        var twitterGAgent = await _gAgentFactory.GetGAgentAsync<ITwitterGAgent>(Guid.NewGuid());
        var twitterGAgentEvents = await twitterGAgent.GetAllSubscribedEventsAsync();
        await routerGAgent.AddAgentDescription(twitterGAgent.GetType(), twitterGAgentEvents);
        
        var groupGAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        await groupGAgent.RegisterAsync(blockChainGAgent);
        await groupGAgent.RegisterAsync(routerGAgent);
        await groupGAgent.RegisterAsync(twitterGAgent);
        
        var state = await routerGAgent.GetStateAsync();
        await groupGAgent.PublishEventAsync(new BeginTaskGEvent()
        {
           TaskDescription = "I want to post a tweet about the current price of bitcoin"
        });
        
   
        await Task.Delay(100000);
    }
}