using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.Core.State;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.Provider;
using GroupChat.GAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.GAgents;

public abstract class MCPGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GroupMemberGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    where TState : MCPGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : MCPGAgentConfig
{
    private readonly IMCPClientProvider _mcpClientProvider;

    protected MCPGAgentBase()
    {
        _mcpClientProvider = ServiceProvider.GetRequiredService<IMCPClientProvider>();
    }

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        RaiseEvent(new SetConfigurationLogEvent
        {
            EnableToolDiscovery = configuration.EnableToolDiscovery,
            RequestTimeout = configuration.RequestTimeout
        });

        if (configuration.Server != null)
        {
            RaiseEvent(new AddMCPServerLogEvent { ServerConfig = configuration.Server });
        }

        await ConfirmEvents();

        await InitializeMCPServersAsync();
    }

    protected override void GroupMemberTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddMCPServerLogEvent addServerEvent:
                State.ServerConfigs.Add(addServerEvent.ServerConfig);
                break;

            case UpdateServerStateLogEvent updateStateEvent:
                State.ServerStates[updateStateEvent.ServerName] = updateStateEvent.ServerState;
                break;

            case RecordToolCallLogEvent recordCallEvent:
                State.TotalToolCalls++;
                State.LastToolCallTime = recordCallEvent.CallTime;
                break;

            case UpdateAvailableToolsLogEvent updateToolsEvent:
                var keysToRemove = State.AvailableTools.Keys
                    .Where(k => k.StartsWith($"{updateToolsEvent.ServerName}."))
                    .ToList();
                foreach (var key in keysToRemove)
                {
                    State.AvailableTools.Remove(key);
                }

                foreach (var tool in updateToolsEvent.Tools)
                {
                    tool.ServerName = updateToolsEvent.ServerName;
                    State.AvailableTools[$"{updateToolsEvent.ServerName}.{tool.Name}"] = tool;
                }

                if (State.ServerStates.ContainsKey(updateToolsEvent.ServerName))
                {
                    State.ServerStates[updateToolsEvent.ServerName].RegisteredTools =
                        updateToolsEvent.Tools.Select(t => t.Name).ToList();
                }

                break;

            case SetConfigurationLogEvent configEvent:
                State.EnableToolDiscovery = configEvent.EnableToolDiscovery;
                State.RequestTimeout = configEvent.RequestTimeout;
                break;
        }

        MCPTransitionState(state, @event);
    }

    protected virtual void MCPTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        // Derived classes can override this method
    }

    public Task<Dictionary<string, MCPToolInfo>> GetAvailableToolsAsync()
    {
        return Task.FromResult(State.AvailableTools);
    }

    public Task<List<MCPServerState>> GetServerStatesAsync()
    {
        return Task.FromResult(State.ServerStates.Values.ToList());
    }

    public async Task<MCPToolResponseEvent> CallToolAsync(string serverName, string toolName,
        Dictionary<string, object> arguments)
    {
        var toolCallEvent = new MCPToolCallEvent
        {
            ServerName = serverName,
            ToolName = toolName,
            Arguments = arguments,
            RequestId = Guid.NewGuid()
        };

        return await HandleEventAsync(toolCallEvent);
    }

    private async Task InitializeMCPServersAsync()
    {
        foreach (var serverConfig in State.ServerConfigs)
        {
            try
            {
                var client = await _mcpClientProvider.GetOrCreateClientAsync(serverConfig);

                client.ConnectionStatusChanged += async (sender, args) =>
                {
                    await UpdateServerStatusAsync(args.ServerName, args.IsConnected);
                };

                var connected = await client.ConnectAsync();

                // Update server status first
                await UpdateServerStatusAsync(serverConfig.ServerName, connected);

                if (connected && State.EnableToolDiscovery)
                {
                    try
                    {
                        var tools = await client.DiscoverToolsAsync();
                        await UpdateServerToolsAsync(serverConfig.ServerName, tools);

                        Logger.LogInformation("Successfully discovered {ToolCount} tools from {ServerName}",
                            tools.Count, serverConfig.ServerName);
                    }
                    catch (Exception toolEx)
                    {
                        // Tool discovery failure shouldn't mark the server as disconnected
                        Logger.LogWarning(toolEx,
                            "Failed to discover tools from {ServerName}, but server is still connected",
                            serverConfig.ServerName);

                        // Update with empty tool list
                        await UpdateServerToolsAsync(serverConfig.ServerName, new List<MCPToolInfo>());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize MCP server {ServerName}", serverConfig.ServerName);
                await UpdateServerStatusAsync(serverConfig.ServerName, false);
            }
        }
    }

    [EventHandler]
    public async Task<MCPToolResponseEvent> HandleEventAsync(MCPToolCallEvent @event)
    {
        try
        {
            Logger.LogInformation("Calling MCP tool {ToolName} on server {ServerName}",
                @event.ToolName, @event.ServerName);

            var serverConfig = State.ServerConfigs.FirstOrDefault(s => s.ServerName == @event.ServerName);
            if (serverConfig == null)
            {
                return new MCPToolResponseEvent
                {
                    RequestId = @event.RequestId,
                    Success = false,
                    ErrorMessage = $"Server {@event.ServerName} not found",
                    ServerName = @event.ServerName,
                    ToolName = @event.ToolName
                };
            }

            var client = await _mcpClientProvider.GetOrCreateClientAsync(serverConfig);

            var actualToolName = @event.ToolName;
            if (@event.ToolName.StartsWith($"{@event.ServerName}."))
            {
                actualToolName = @event.ToolName.Substring(@event.ServerName.Length + 1);
            }

            using var cts = new CancellationTokenSource(State.RequestTimeout);
            var result = await client.CallToolAsync(actualToolName, @event.Arguments);

            RaiseEvent(new RecordToolCallLogEvent
            {
                ServerName = @event.ServerName,
                ToolName = @event.ToolName,
                CallTime = DateTime.UtcNow
            });

            await ConfirmEvents();

            Logger.LogInformation("MCP tool {ToolName} completed with success: {Success}",
                @event.ToolName, result.Success);

            return new MCPToolResponseEvent
            {
                RequestId = @event.RequestId,
                Success = result.Success,
                Result = result.Data,
                ErrorMessage = result.ErrorMessage,
                ServerName = @event.ServerName,
                ToolName = @event.ToolName
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling MCP tool {ToolName} on server {ServerName}",
                @event.ToolName, @event.ServerName);

            return new MCPToolResponseEvent
            {
                RequestId = @event.RequestId,
                Success = false,
                ErrorMessage = ex.Message,
                ServerName = @event.ServerName,
                ToolName = @event.ToolName
            };
        }
    }

    [EventHandler]
    public async Task<MCPToolsDiscoveredEvent> HandleEventAsync(MCPDiscoverToolsEvent @event)
    {
        try
        {
            Logger.LogInformation("Discovering tools on server {ServerName}", @event.ServerName);

            var serverConfig = State.ServerConfigs.FirstOrDefault(s => s.ServerName == @event.ServerName);
            if (serverConfig == null)
            {
                return new MCPToolsDiscoveredEvent
                {
                    ServerName = @event.ServerName,
                    Tools = []
                };
            }

            var client = await _mcpClientProvider.GetOrCreateClientAsync(serverConfig);
            var tools = await client.DiscoverToolsAsync();

            await UpdateServerToolsAsync(@event.ServerName, tools);

            Logger.LogInformation("Discovered {ToolCount} tools on server {ServerName}",
                tools.Count, @event.ServerName);

            return new MCPToolsDiscoveredEvent
            {
                ServerName = @event.ServerName,
                Tools = tools
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error discovering tools on server {ServerName}", @event.ServerName);

            return new MCPToolsDiscoveredEvent
            {
                ServerName = @event.ServerName,
                Tools = []
            };
        }
    }

    private async Task UpdateServerToolsAsync(string serverName, List<MCPToolInfo> tools)
    {
        RaiseEvent(new UpdateAvailableToolsLogEvent
        {
            ServerName = serverName,
            Tools = tools
        });

        await ConfirmEvents();
    }

    private async Task UpdateServerStatusAsync(string serverName, bool isConnected)
    {
        var serverState = State.ServerStates.ContainsKey(serverName)
            ? State.ServerStates[serverName]
            : new MCPServerState { ServerName = serverName };

        serverState.IsConnected = isConnected;
        serverState.LastConnectedTime = isConnected ? DateTime.UtcNow : serverState.LastConnectedTime;

        RaiseEvent(new UpdateServerStateLogEvent
        {
            ServerName = serverName,
            ServerState = serverState
        });

        await ConfirmEvents();

        await PublishAsync(new MCPServerStatusEvent
        {
            ServerName = serverName,
            IsConnected = isConnected,
            StatusMessage = isConnected ? "Connected" : "Disconnected"
        });
    }

    [GenerateSerializer]
    public class MCPGAgentBaseStateLogEvent : StateLogEventBase<TStateLogEvent>;

    [GenerateSerializer]
    public class AddMCPServerLogEvent : MCPGAgentBaseStateLogEvent
    {
        [Id(0)] public MCPServerConfig ServerConfig { get; set; } = new();
    }

    [GenerateSerializer]
    public class UpdateServerStateLogEvent : MCPGAgentBaseStateLogEvent
    {
        [Id(0)] public string ServerName { get; set; } = string.Empty;
        [Id(1)] public MCPServerState ServerState { get; set; } = new();
    }

    [GenerateSerializer]
    public class RecordToolCallLogEvent : MCPGAgentBaseStateLogEvent
    {
        [Id(0)] public string ServerName { get; set; } = string.Empty;
        [Id(1)] public string ToolName { get; set; } = string.Empty;
        [Id(2)] public DateTime CallTime { get; set; }
    }

    [GenerateSerializer]
    public class UpdateAvailableToolsLogEvent : MCPGAgentBaseStateLogEvent
    {
        [Id(0)] public string ServerName { get; set; } = string.Empty;
        [Id(1)] public List<MCPToolInfo> Tools { get; set; } = new();
    }

    [GenerateSerializer]
    public class SetConfigurationLogEvent : MCPGAgentBaseStateLogEvent
    {
        [Id(0)] public bool EnableToolDiscovery { get; set; }
        [Id(1)] public TimeSpan RequestTimeout { get; set; }
    }
}