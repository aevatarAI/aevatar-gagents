using Aevatar.GAgents.GraphRag.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Neo4j;

[DependsOn(
    typeof(AevatarGAgentsGraphRagContractModule))]
public class AevatarGAgentsNeo4jModule : AbpModule
{
    // public override void ConfigureServices(ServiceConfigurationContext context)
    // {
    //     var configuration = context.Services.GetConfiguration();
    //     
    //     context.Services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
    //         configuration["Neo4j:Uri"],
    //         AuthTokens.Basic(
    //             configuration["Neo4j:User"],
    //             configuration["Neo4j:Password"]
    //         )
    //     ));
    //
    //     context.Services.AddTransient<IGraphRagStore, Neo4jGraphRagStore>();
    // }
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentsNeo4jModule>();
        });
        var configuration = context.Services.GetConfiguration();
        context.Services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
            configuration["Neo4j:Uri"],
            AuthTokens.Basic(
                configuration["Neo4j:User"],
                configuration["Neo4j:Password"]
            )
        ));
        
        context.Services.AddTransient<IGraphRagStore, Neo4jGraphRagStore>();
    }
}