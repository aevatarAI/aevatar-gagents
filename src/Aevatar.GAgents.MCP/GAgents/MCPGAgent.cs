using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.State;

namespace Aevatar.GAgents.MCP.GAgents;

[GenerateSerializer]
public class MCPGAgentStateLogEvent : StateLogEventBase<MCPGAgentStateLogEvent>;

[GAgent("mcp", "aevatar")]
public class MCPGAgent : MCPGAgentBase<MCPGAgentState, MCPGAgentStateLogEvent, EventBase, MCPGAgentConfig>,
    IMCPGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("MCP GAgent for interacting with Model Context Protocol servers");
    }
}