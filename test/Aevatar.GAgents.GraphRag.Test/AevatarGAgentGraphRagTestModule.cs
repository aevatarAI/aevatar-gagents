using Aevatar.GAgents.Neo4j;
using Aevatar.GAgents.Neo4j.Extensions;
using Aevatar.GAgents.Neo4j.Options;
using Aevatar.GAgents.TestBase;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.GraphRag.Test;

[DependsOn(
    typeof(AevatarGAgentsNeo4jModule),
    typeof(AevatarGAgentTestBaseModule)
)]
public class AevatarGAgentGraphRagTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarGAgentGraphRagTestModule>(); });
        context.Services.AddSingleton(new ApplicationPartManager());
    }
}