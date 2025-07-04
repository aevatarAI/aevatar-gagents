using Aevatar.GAgents.MCP;
using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.MCP.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule),
    typeof(AevatarGAgentsMCPModule)
)]
public class AevatarMCPTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
    }
}