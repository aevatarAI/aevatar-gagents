// See https://aka.ms/new-console-template for more information

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.Basic.GroupGAgent;
using Google.Protobuf.WellKnownTypes;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Blackboard;
using GroupChat.Grain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams("InMemoryStreamProvider");
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.StartAsync();


IClusterClient client = host.Services.GetRequiredService<IClusterClient>();
var groupAgent = client.GetGrain<IStateGAgent<GroupGAgentState>>(Guid.NewGuid());

var blackboard =  client.GetGrain<IBlackboardGAgent>(Guid.NewGuid());
var jack = client.GetGrain<IWorker>(Guid.NewGuid());
await jack.SetMemberName("Jack");
var fred = client.GetGrain<IWorker>(Guid.NewGuid());
await fred.SetMemberName("Fred");

var leader = client.GetGrain<ILeader>(Guid.NewGuid());
await leader.SetMemberName("Aera");

await groupAgent.RegisterAsync(blackboard);
await groupAgent.RegisterAsync(jack);
await groupAgent.RegisterAsync(fred);
await groupAgent.RegisterAsync(leader);

await blackboard.SetTopic("Will Artificial Intelligence Replace Human Creativity ?");

await Task.Delay(TimeSpan.FromSeconds(1000));