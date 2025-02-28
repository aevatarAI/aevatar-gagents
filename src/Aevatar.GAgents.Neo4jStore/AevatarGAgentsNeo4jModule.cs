using Aevatar.GAgents.GraphRag.Abstractions;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Neo4jStore;

[DependsOn(
    typeof(AevatarGAgentsGraphRagContractModule))]
public class AevatarGAgentsNeo4JStoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentsNeo4JStoreModule>();
        });
    }
}