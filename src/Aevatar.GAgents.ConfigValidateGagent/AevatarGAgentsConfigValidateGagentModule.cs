using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Aevatar.GAgents.Common;

namespace Aevatar.GAgents.ConfigValidateGagent;

[DependsOn(
    typeof(AevatarGAgentsCommonModule)
    )]
public class AevatarGAgentsConfigValidateGagentModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Configure services specific to ConfigValidateGagent module
        // Additional configuration can be added here as needed
    }
}