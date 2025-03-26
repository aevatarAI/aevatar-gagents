using System.Linq.Dynamic.Core.Tokenizer;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.LogEvent;
using GroupChat.GAgent.Feature.Blackboard;
using GroupChat.GAgent.Feature.Coordinator;

namespace Aevatar.GAgents.GroupChat.Feature.Extension;

public static class IGAgentExtension
{
    public static async Task<bool> AddGroupChat(this IGAgent agent, IClusterClient clusterClient, string topic)
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

    public static async Task AddWorkflowGroupChat(this IGAgent agent, IClusterClient clusterClient, List<WorkflowUnitDto> workflowUnitList)
    {
        var blackboard = clusterClient.GetGrain<IBlackboardGAgent>(Guid.NewGuid());
        await agent.RegisterAsync(blackboard);
        foreach (var item in workflowUnitList)
        {
            var grainId = GrainId.Parse(item.GrainId);
            var workUnit = clusterClient.GetGrain<IGAgent>(grainId);
            await agent.RegisterAsync(workUnit);
        }
        
        var workflowCoordinator = clusterClient.GetGrain<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        await workflowCoordinator.ConfigAsync(new WorkflowCoordinatorConfigDto()
        {
            WorkflowUnitList = workflowUnitList,
            BlackBoardId = blackboard.GetPrimaryKey(),
        });
        
        await agent.RegisterAsync(workflowCoordinator);
    }
}