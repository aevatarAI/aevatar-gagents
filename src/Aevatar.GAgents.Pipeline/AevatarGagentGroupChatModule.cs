using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Pipeline;


[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsPipelinetModule: AbpModule
{
    
}