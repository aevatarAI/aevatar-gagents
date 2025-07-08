using System.Collections.Generic;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Test;

/// <summary>
/// 常用MCP服务器配置示例
/// 这些是实际可用的MCP服务器，可以在测试和生产环境中使用
/// </summary>
public static class CommonMCPServersExample
{
    /// <summary>
    /// 获取常用的MCP服务器配置列表
    /// </summary>
    public static List<MCPServerConfig> GetCommonMCPServers()
    {
        return new List<MCPServerConfig>
        {
            // 1. 文件系统操作
            new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" },
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
            Servers = new List<MCPServerConfig>
            {
                // 只包含不需要外部依赖的服务器
                new MCPServerConfig
                {
                    ServerName = "filesystem",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" }
                },
                new MCPServerConfig
                {
                    ServerName = "memory",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-memory" }
                },
                new MCPServerConfig
                {
                    ServerName = "time",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-time" }
                }
            },
            EnableToolDiscovery = true,
            RequestTimeout = System.TimeSpan.FromSeconds(30)
        };
    }
}