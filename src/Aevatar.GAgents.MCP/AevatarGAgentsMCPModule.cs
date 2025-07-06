using Aevatar.GAgents.MCP.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.MCP;

public class AevatarGAgentsMCPModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // 注册MCP Client Provider依赖
        // 注册HttpClient用于HTTP transport
        context.Services.AddHttpClient<MCPClientProviderSelector>();
        
        // 使用Provider选择器，自动根据配置选择合适的Provider
        context.Services.TryAddSingleton<IMCPClientProvider, MCPClientProviderSelector>();
        
        // 在开发/测试环境中，你可以切换到Mock实现：
        // context.Services.TryAddSingleton<IMCPClientProvider, MockMCPClientProvider>();
        
        // 如果需要直接使用特定的Provider：
        // 1. Stdio-based provider only:
        //    context.Services.TryAddSingleton<IMCPClientProvider, StdioMCPClientProvider>();
        //
        // 2. HTTP-based provider only:
        //    context.Services.TryAddSingleton<IMCPClientProvider, RealMCPClientProvider>();
    }
}