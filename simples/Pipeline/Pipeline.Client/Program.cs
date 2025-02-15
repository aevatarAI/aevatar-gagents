// See https://aka.ms/new-console-template for more information

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Pipeline.GAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pipeline.Grains;

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

//
// var gAgentFactory = host.Services.GetRequiredService<IGAgentFactory>();
// var gAgentManager = host.Services.GetRequiredService<IGAgentManager>();
// var allAgentType = gAgentManager.GetAvailableGAgentTypes();

IClusterClient client = host.Services.GetRequiredService<IClusterClient>();
var pipelineAgent = client.GetGrain<IPipelineGAgent>(Guid.NewGuid());
var test = client.GetGrain<ITest>(Guid.NewGuid());
var designer = client.GetGrain<IDesignerGAgent>(Guid.NewGuid());
var product = client.GetGrain<IProductGAgent>(Guid.NewGuid());
var programmer = client.GetGrain<IProgrammerGAgent>(Guid.NewGuid());
await pipelineAgent.OrchestrateJobAsync(product, designer);
await pipelineAgent.OrchestrateJobAsync(designer, programmer);

await pipelineAgent.StartAsync(new ProductRequirements() { Content = "this is product requirement" });

