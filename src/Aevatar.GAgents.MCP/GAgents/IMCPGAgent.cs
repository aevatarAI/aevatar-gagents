using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.State;

namespace Aevatar.GAgents.MCP.GAgents;

public interface IMCPGAgent : IStateGAgent<MCPGAgentState>
{
    Task<Dictionary<string, MCPToolInfo>> GetAvailableToolsAsync();
    Task<List<MCPServerState>> GetServerStatesAsync();
}
