using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.MCP.McpClient;
using Aevatar.GAgents.MCP.Options;
using ModelContextProtocol.Client;
using Moq;

namespace Aevatar.GAgents.MCP.Test;

public class MockMcpClientProvider : IMcpClientProvider
{
    private readonly Dictionary<string, IMcpClient> _mockClients = new();
    
    public McpClientType ClientType => McpClientType.Stdio;

    public async Task<IMcpClient> GetOrCreateClientAsync(MCPServerConfig config)
    {
        if (_mockClients.TryGetValue(config.ServerName, out var existingClient))
        {
            return existingClient;
        }

        // 创建一个Mock的MCP客户端
        var mockClient = new Mock<IMcpClient>();
        
        // 设置基本的Mock行为
        mockClient.Setup(x => x.ListToolsAsync())
            .ReturnsAsync(new List<McpClientTool>());
            
        mockClient.Setup(x => x.PingAsync())
            .Returns(Task.CompletedTask);
            
        mockClient.Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var client = mockClient.Object;
        _mockClients[config.ServerName] = client;
        
        return await Task.FromResult(client);
    }

    public Task DisconnectClientAsync(string serverName)
    {
        _mockClients.Remove(serverName);
        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync(string serverName)
    {
        return Task.FromResult(_mockClients.ContainsKey(serverName));
    }
} 