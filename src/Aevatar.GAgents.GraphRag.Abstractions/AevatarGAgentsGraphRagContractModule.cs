using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.GraphRag.Abstractions;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsGraphRagContractModule : AbpModule
{
    
}