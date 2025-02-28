using Aevatar.GAgents.Neo4jStore;
using Aevatar.GAgents.Neo4jStore.Extensions;
using Aevatar.GAgents.Neo4jStore.Options;
using Aevatar.GAgents.TestBase;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.GraphRag.Test;

[DependsOn(
    typeof(AevatarGAgentsNeo4JStoreModule),
    typeof(AevatarGAgentTestBaseModule)
)]
public class AevatarGAgentGraphRagTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarGAgentGraphRagTestModule>(); });
        context.Services.AddSingleton(new ApplicationPartManager());
        
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<Neo4jDriverConfig>(configuration.GetSection("Neo4j"));
        
        context.Services.AddNeo4JStore();
    }
}