using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.MCP.Core.Options;

namespace Aevatar.GAgents.MCP.Core.Extensions;

// ReSharper disable InconsistentNaming
public static class GAgentFactoryExtensions
{
    public static async Task<IConfigManagerGAgent> GetSystemLLMConfigGAgent(this IGAgentFactory gAgentFactory)
    {
        var configGuid = typeof(SystemLLMConfigOptions).FullName!.ToGuid();
        return await gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(configGuid);
    }

    public static async Task<IConfigManagerGAgent> GetMCPServerConfigGAgent(this IGAgentFactory gAgentFactory)
    {
        var configGuid = typeof(MCPServerOptions).FullName!.ToGuid();
        return await gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(configGuid);
    }
}