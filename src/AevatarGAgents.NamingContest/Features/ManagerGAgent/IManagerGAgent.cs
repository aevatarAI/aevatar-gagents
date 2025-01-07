using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.NamingContest.ManagerGAgent;

public interface IManagerGAgent:IGAgent,IGrainWithGuidKey
{
    Task InitAgentsAsync(InitAgentMessageSEvent initAgentMessageSEvent);
    
    Task InitGroupInfoAsync(InitNetWorkMessageSEvent initNetWorkMessageSEvent,string groupAgentId );

    Task ClearAllAgentsAsync();

    Task<ManagerAgentState> GetManagerAgentStateAsync();
}