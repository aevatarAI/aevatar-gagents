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

IClusterClient client = host.Services.GetRequiredService<IClusterClient>();
var pipelineAgent = client.GetGrain<IPipelineGAgent>(Guid.NewGuid());
var designer = client.GetGrain<IDesignerGAgent>(Guid.NewGuid());
var product = client.GetGrain<IProductGAgent>(Guid.NewGuid());
var programmer1 = client.GetGrain<IProgrammerGAgent>(Guid.NewGuid());
var programmer2 = client.GetGrain<IProgrammerGAgent>(Guid.NewGuid());
await pipelineAgent.OrchestrateJobAsync(product, designer);
await pipelineAgent.OrchestrateJobAsync(designer, programmer1);
await pipelineAgent.OrchestrateJobAsync(designer, programmer2);

await pipelineAgent.StartAsync(new ProductRequirements() { Content = "this is product requirement" });

