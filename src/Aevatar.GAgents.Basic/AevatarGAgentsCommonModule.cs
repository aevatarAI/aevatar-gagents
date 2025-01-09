using Abp.AutoMapper;
using Abp.Modules;

namespace Aevatar.GAgents.Common;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsCommonModule:AbpModule
{
    
}