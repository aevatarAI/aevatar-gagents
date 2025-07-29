using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Plugin;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.GEvents;
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
            ServerConfig = new MCPServerConfig()
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"],
                Env = new Dictionary<string, string>
                {
                    ["NODE_ENV"] = "production"
                }
            }
        };

        // Act
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Assert
        var state = await mcpGAgent.GetStateAsync();
        state.MCPServerConfig.ServerName.ShouldBe("filesystem");
        
    }

    [Fact]
    public async Task HandleEventAsync_MCPToolCallEvent_Should_Return_Response()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            ServerConfig = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["-y", "@modelcontextprotocol/server-filesystem"]
            }
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

        var responseJson =
            await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));
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
            ServerConfig = new MCPServerConfig
            {
                ServerName = "filesystem2",
                Command = "npx",
                Args = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"]
            },
        };

        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);

        // Act
        var discoverEvent = new MCPDiscoverToolsEvent();

        var responseJson =
            await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, discoverEvent, typeof(MCPToolsDiscoveredEvent));
        var response = JsonConvert.DeserializeObject<MCPToolsDiscoveredEvent>(responseJson, new JsonSerializerSettings
        {
            Converters = { new GrainIdConverter() }
        });

        // Assert
        response.ShouldNotBeNull();
        response.ServerName.ShouldBe("sqlite");
        response.Tools.ShouldNotBeNull();
        response.Tools.Count.ShouldBeGreaterThan(0);

        // Verify available tools
        var availableTools = await mcpGAgent.GetAvailableToolsAsync();
        availableTools.Count.ShouldBeGreaterThan(0);
        availableTools.Any(k => k.ServerName.StartsWith("sqlite.")).ShouldBeTrue();
    }

    [Fact]
    public async Task MCPGAgent_Should_Handle_Server_Not_Found_Error()
    {
        // Arrange
        var config = new MCPGAgentConfig
        {
            ServerConfig = new MCPServerConfig
            {
                ServerName = "test-server",
                Command = "test"
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

        var responseJson =
            await _gAgentExecutor.ExecuteGAgentEventHandler(mcpGAgent, toolCallEvent, typeof(MCPToolResponseEvent));
        var response = JsonConvert.DeserializeObject<MCPToolResponseEvent>(responseJson,
            new JsonSerializerSettings
            {
                Converters = { new GrainIdConverter() }
            });

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldNotBeNull();
        response.ErrorMessage.ShouldContain("not found");
    }
}