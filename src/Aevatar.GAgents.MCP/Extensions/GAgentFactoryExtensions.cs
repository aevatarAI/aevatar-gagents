using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Extensions;

public static class GAgentFactoryExtensions
{
    public static async Task<IMCPGAgent> GetFilesystemMCPGAgent(this IGAgentFactory gAgentFactory,
        params string[] paths)
    {
        if (DefaultMCPServers.Configs.TryGetValue("filesystem", out var config))
        {
            if (!paths.IsNullOrEmpty())
            {
                config.Args = ["-y", "@modelcontextprotocol/server-filesystem"];
                config.Args.AddRange(paths);
            }

            return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
            {
                MemberName = "filesystem-mcp-server",
                ServerConfig = config
            });
        }

        throw new MCPServerConfigNotFoundException("MCP Server config of filesystem not found.");
    }
}