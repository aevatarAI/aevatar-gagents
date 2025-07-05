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
        
        // 注册MCP Client Provider
        // 注册HttpClient用于HTTP transport
        context.Services.AddHttpClient<RealMCPClientProvider>();
        
        // 使用真实的MCP Client Provider
        context.Services.TryAddSingleton<IMCPClientProvider, RealMCPClientProvider>();
        
        // 在开发/测试环境中，你可以切换到Mock实现：
        // context.Services.TryAddSingleton<IMCPClientProvider, MockMCPClientProvider>();
        
        // 未来可以添加其他transport的支持：
        // 1. Stdio-based provider (for stdio transport):
        //    context.Services.AddSingleton<IMCPClientProvider, StdioMCPClientProvider>();
        //
        // 2. WebSocket-based provider:
        //    context.Services.AddSingleton<IMCPClientProvider, WebSocketMCPClientProvider>();
    }
}