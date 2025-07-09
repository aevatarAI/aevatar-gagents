using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;

namespace Aevatar.GAgents.Twitter.GAgents.DirectAIAgent;

public interface IDirectAIGAgent : IAIGAgent, IStateGAgent<DirectAIGAgentState>
{
    Task<string?> ChatAsync(string message);
    Task<string> GetLastResponseAsync();
} 