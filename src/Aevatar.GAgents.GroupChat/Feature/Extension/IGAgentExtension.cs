using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Blackboard;
using GroupChat.GAgent.Feature.Coordinator;

namespace Aevatar.GAgents.GroupChat.Feature.Extension;

public static class IGAgentExtension
{
    public static async Task<bool> AddBlackboard(this IGAgent agent, IClusterClient clusterClient, string topic)
    {
        var blackboard = clusterClient.GetGrain<IBlackboardGAgent>(Guid.NewGuid());
        if (await blackboard.SetTopic(topic) == false)
        {
            return false;
        }

        await agent.RegisterAsync(blackboard);
        var coordinatorGAgent = clusterClient.GetGrain<ICoordinatorGAgent>(blackboard.GetPrimaryKey());
        await agent.RegisterAsync(coordinatorGAgent);

        await coordinatorGAgent.StartAsync(blackboard.GetPrimaryKey());

        return true;
    }
}