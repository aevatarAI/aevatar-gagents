using Aevatar.GAgents.MCP.Provider;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.MCP;

public class AevatarGAgentsMCPModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // 注册MCP Client Provider
        // 暂时使用占位符实现，实际需要实现真正的MCP Client Provider
        context.Services.AddSingleton<IMCPClientProvider, MockMCPClientProvider>();
    }
}