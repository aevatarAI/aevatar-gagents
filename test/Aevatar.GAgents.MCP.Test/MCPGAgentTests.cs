using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Plugin;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.MCP.Test;

public class MCPGAgentTests : AevatarMCPTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentExecutor _gAgentExecutor;

    public MCPGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentExecutor = GetRequiredService<IGAgentExecutor>();
    }

    [Fact]
    public async Task ConfigureAsync_Should_Initialize_MCP_Servers()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            Servers =
            [
                new MCPServerConfig()
                {
                    ServerName = "filesystem",
                    Command = "npx",
                    Args = ["-y", "@modelcontextprotocol/server-filesystem"],
                    Environment = new Dictionary<string, string>
                    {
                        ["NODE_ENV"] = "production"
                    }
                },

                new MCPServerConfig()
                {
                    ServerName = "github",
                    Command = "npx",
                    Args = ["-y", "@modelcontextprotocol/server-github"],
                    Environment = new Dictionary<string, string>
                    {
                        ["GITHUB_TOKEN"] = "test-token"
                    }
                }
            ],
            EnableToolDiscovery = true
        };

        // Act
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Assert
        var serverStates = await mcpGAgent.GetServerStatesAsync();
        serverStates.Count.ShouldBe(2);
        serverStates.Any(s => s.ServerName == "filesystem").ShouldBeTrue();
        serverStates.Any(s => s.ServerName == "github").ShouldBeTrue();
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_Should_Return_Response()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            Servers =
            [
                new MCPServerConfig
                {
                    ServerName = "filesystem",
                    Command = "npx",
                    Args = ["-y", "@modelcontextprotocol/server-filesystem"]
                }
            ]
        };

        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act
        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = "read_file",
            Arguments = new Dictionary<string, object>
            {
                ["path"] = "/test/path.txt"
            }
        };

        var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, toolCallEvent);
        _testOutputHelper.WriteLine(responseJson);
        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson, new JsonSerializerSettings
        {
            Converters = { new GrainIdConverter() }
        });

        // Assert
        response.ShouldNotBeNull();
        response.ServerName.ShouldBe("filesystem");
        response.ToolName.ShouldBe("read_file");
        response.Success.ShouldBeTrue();
        response.Result.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleEventAsync_MCPDiscoverToolsEvent_Should_Return_Tools()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            Servers = new List<MCPServerConfig>
            {
                new MCPServerConfig
                {
                    ServerName = "sqlite",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-sqlite", "memory:" }
                }
            },
            EnableToolDiscovery = true
        };

        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act
        var discoverEvent = new MCPDiscoverToolsEvent
        {
            ServerName = "sqlite"
        };

        var response = await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, discoverEvent);

        // Assert
        response.ShouldNotBeNull();
        // response.ServerName.ShouldBe("sqlite");
        // response.Tools.ShouldNotBeNull();
        // response.Tools.Count.ShouldBeGreaterThan(0);

        // Verify available tools
        var availableTools = await mcpGAgent.GetAvailableToolsAsync();
        availableTools.Count.ShouldBeGreaterThan(0);
        availableTools.Keys.Any(k => k.StartsWith("sqlite.")).ShouldBeTrue();
    }

    [Fact]
    public async Task Multiple_MCP_Servers_Can_Work_Together()
    {
        // Arrange - 配置多个实际可用的MCP服务器
        var config = new MCPGAgentConfig
        {
            Servers = new List<MCPServerConfig>
            {
                // 文件系统服务器
                new MCPServerConfig
                {
                    ServerName = "filesystem",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" }
                },
                // SQLite内存数据库服务器
                new MCPServerConfig
                {
                    ServerName = "sqlite",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-sqlite", "memory:" }
                },
                // Fetch HTTP请求服务器
                new MCPServerConfig
                {
                    ServerName = "fetch",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-fetch" }
                }
            },
            EnableToolDiscovery = true,
            RequestTimeout = TimeSpan.FromSeconds(30)
        };

        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act - 测试每个服务器的工具调用

        // 1. 文件系统操作
        var fsListEvent = new MCPToolCallEvent
        {
            ServerName = "filesystem",
            ToolName = "list_directory",
            Arguments = new Dictionary<string, object>
            {
                ["path"] = "/tmp"
            }
        };
        var fsResponse = await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, fsListEvent);

        // 2. SQLite查询
        var sqlCreateEvent = new MCPToolCallEvent
        {
            ServerName = "sqlite",
            ToolName = "execute_query",
            Arguments = new Dictionary<string, object>
            {
                ["query"] = "CREATE TABLE test (id INTEGER PRIMARY KEY, name TEXT)"
            }
        };
        var sqlResponse = await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, sqlCreateEvent);

        // 3. HTTP请求
        var fetchEvent = new MCPToolCallEvent
        {
            ServerName = "fetch",
            ToolName = "fetch",
            Arguments = new Dictionary<string, object>
            {
                ["url"] = "https://api.github.com"
            }
        };
        var fetchResponse = await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, fetchEvent);

        // Assert
        fsResponse.ShouldNotBeNull();
        // fsResponse.Success.ShouldBeTrue();
        // sqlResponse.Success.ShouldBeTrue();
        // fetchResponse.Success.ShouldBeTrue();

        // 验证所有服务器都已连接
        var serverStates = await mcpGAgent.GetServerStatesAsync();
        serverStates.Count.ShouldBe(3);
        serverStates.All(s => s.IsConnected).ShouldBeTrue();
    }

    [Fact]
    public async Task MCPGAgent_Should_Handle_Server_Not_Found_Error()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            Servers = new List<MCPServerConfig>
            {
                new MCPServerConfig
                {
                    ServerName = "test-server",
                    Command = "test"
                }
            }
        };

        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act
        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = "non-existent-server",
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object>()
        };

        var response = await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, toolCallEvent);

        // Assert
        response.ShouldNotBeNull();
        // response.Success.ShouldBeFalse();
        // response.ErrorMessage.ShouldContain("not found");
    }
}