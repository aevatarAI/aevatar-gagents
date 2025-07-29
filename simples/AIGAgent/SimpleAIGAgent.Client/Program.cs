using System.Text.Json;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimpleAIGAgent.Grains.Agents.Chat;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams("InMemoryStreamProvider");
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();
builder.ConfigureServices((context, service) => { service.AddSingleton<IGAgentFactory, GAgentFactory>(); });

using IHost host = builder.Build();
await host.StartAsync();

IGAgentFactory agentFactory = host.Services.GetRequiredService<IGAgentFactory>();

IClusterClient client = host.Services.GetRequiredService<IClusterClient>();

var chatAiAgent = await agentFactory.GetGAgentAsync<IChatAIGAgent>();

var descriptionInfo = new AgentDescriptionInfo
{
    Id = "ChatAIGAgent",
    Name = "Twitter Chat AI Agent",
    L1Description = "AI-powered chat agent specifically designed for Twitter social interactions and conversations",
    L2Description =
        "Specialized conversational AI agent optimized for Twitter's social context, handling mentions, DMs, and public conversations with personality adaptation and engagement optimization.",
    Category = "Social",
    Capabilities = new List<string>
        { "twitter-conversation", "mention-handling", "dm-management", "engagement-optimization" },
    Tags = new List<string> { "twitter", "chat", "ai", "conversation" }
};

var prompt = JsonSerializer.Serialize(descriptionInfo);
var result = await chatAiAgent.ChatAsync(prompt);