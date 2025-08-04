using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.Core.State;
using Aevatar.GAgents.MCP.Options;

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

    public static async Task<IMCPGAgent?> GetMCPGAgentAsync(this IGAgentFactory gAgentFactory, string mcpServerName)
    {
        var configManagerGAgent = await gAgentFactory.GetMCPServerConfigGAgent();
        var configResponseEvent = await configManagerGAgent.RequestConfigAsync(new ConfigRequestEvent
        {
            ConfigType = typeof(MCPServerOptions).FullName!,
            ConfigKey = mcpServerName
        });
        if (!configResponseEvent.Success)
        {
            return null;
        }

        var configDict =
            JsonSerializer.Deserialize<Dictionary<string, MCPServerConfig>>(configResponseEvent.ConfigJson);
        if (configDict != null && configDict.TryGetValue(mcpServerName, out var mcpServerConfig))
        {
            return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
            {
                MemberName = mcpServerName,
                ServerConfig = mcpServerConfig
            });
        }

        return null;
    }
}