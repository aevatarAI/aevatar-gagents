using Abp.AutoMapper;
using Abp.Modules;

namespace AevatarGAgents.Common;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsCommonModule:AbpModule
{
    
}