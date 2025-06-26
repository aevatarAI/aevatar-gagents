// ABOUTME: This file provides ABP module for registering mock LLM services in tests
// ABOUTME: Replaces real AI service dependencies with mock implementations for unit testing

using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.AIGAgent.Test.Modules;

public class MockBrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // Replace real BrainFactory with mock implementation
        services.AddSingleton<IBrainFactory, MockBrainFactory>();
        
        // Optional: Register individual mock brains if needed for direct injection
        services.AddTransient<MockChatBrain>();
        services.AddTransient<MockTextToImageBrain>();
    }
}