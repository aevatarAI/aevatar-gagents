
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Basic;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarDefaultGAgentsModule : AbpModule
{

}