using Aevatar.GAgents.Common;
using Aevatar.GAgents.TestBase;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Basic.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule),
    typeof(AevatarGAgentsCommonModule)
)]
public class AevatarBasicTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // No additional services needed for basic tests
    }
}