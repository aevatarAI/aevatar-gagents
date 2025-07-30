using System.Collections.Generic;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Test;

public static class CommonMCPServersExample
{
    public static List<MCPServerConfig> GetCommonMCPServers()
    {
        return new List<MCPServerConfig>
        {
            // 1. 文件系统操作
            new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"],
                Env = new Dictionary<string, string>
                {
                    ["NODE_ENV"] = "production"
                }
            },

            // 2. GitHub API
            new MCPServerConfig
            {
                ServerName = "github",
                Command = "npx",
                Args = new List<string> { "-y", "@modelcontextprotocol/server-github" },
                Env = new Dictionary<string, string>
                {
                    ["GITHUB_TOKEN"] = "your-github-token" // 需要替换为实际的token
                }
            },

            // 3. SQLite数据库
            new MCPServerConfig
            {
                ServerName = "sqlite",
                Command = "npx",
                Args = new List<string> { "-y", "@modelcontextprotocol/server-sqlite", "memory:" } // 使用内存数据库
            },

            // 4. HTTP Fetch
            new MCPServerConfig
            {
                ServerName = "fetch",
                Command = "npx",
                Args = new List<string> { "-y", "@modelcontextprotocol/server-fetch" }
            },

            // 5. Time (时间工具)
            new MCPServerConfig
            {
                ServerName = "time",
                Command = "npx",
                Args = new List<string> { "-y", "@modelcontextprotocol/server-time" }
            },

            // 6. Memory (键值存储)
            new MCPServerConfig
            {
                ServerName = "memory",
                Command = "npx",
                Args = new List<string> { "-y", "@modelcontextprotocol/server-memory" }
            }
        };
    }

    /// <summary>
    /// 获取用于测试的最小配置集
    /// </summary>
    public static MCPGAgentConfig GetTestConfiguration()
    {
        return new MCPGAgentConfig
        {
            Server = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"]
            },
            EnableToolDiscovery = true,
            RequestTimeout = System.TimeSpan.FromSeconds(30)
        };
    }
}