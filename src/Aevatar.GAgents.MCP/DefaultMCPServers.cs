using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP;

// ReSharper disable once InconsistentNaming
public static class DefaultMCPServers
{
    public const string FilesystemMCPServerName = "filesystem";
    public const string MemoryMCPServerName = "memory";
    
    public static Dictionary<string, MCPServerConfig> Configs = new Dictionary<string, MCPServerConfig>
    {
        [FilesystemMCPServerName] = new()
        {
            ServerName = FilesystemMCPServerName,
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp", "/Users", "/System/Volumes/Data/Users"],
            Description = "File system access - read/write files and directories",
        },
        [MemoryMCPServerName] = new()
        {
            ServerName = MemoryMCPServerName,
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-memory"],
            Description = "",
        },
    };
}