using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.State;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.MCP.GAgents;

[GenerateSerializer]
public class MCPGAgentStateLogEvent : StateLogEventBase<MCPGAgentStateLogEvent>;

[GAgent]
public class MCPGAgent : MCPGAgentBase<MCPGAgentState, MCPGAgentStateLogEvent, EventBase, MCPGAgentConfig>,
    IMCPGAgent
{

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("MCP GAgent for interacting with Model Context Protocol servers");
    }
}