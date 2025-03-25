// See https://aka.ms/new-console-template for more information

using System;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.GroupGAgent;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using GroupChat.GAgent.Dto;
using GroupChat.Grain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;

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

var jack = client.GetGrain<IWorker>(Guid.NewGuid());
await jack.ConfigAsync(new GroupMemberConfigDto(){MemberName="Jack"});
var fred = client.GetGrain<IWorker>(Guid.NewGuid());
await fred.ConfigAsync(new GroupMemberConfigDto(){MemberName="Fred"});

var leader = client.GetGrain<ILeader>(Guid.NewGuid());
await leader.ConfigAsync(new GroupMemberConfigDto(){MemberName="Leader"});

await groupAgent.RegisterAsync(jack);
await groupAgent.RegisterAsync(fred);
await groupAgent.RegisterAsync(leader);
await groupAgent.AddBlackboard(client, "Will Artificial Intelligence Replace Human Creativity ?");

await Task.Delay(TimeSpan.FromSeconds(1000));