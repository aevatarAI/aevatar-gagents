using System.Configuration;
using System.Reflection;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.GEvent;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.LogEvent;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Newtonsoft.Json;
using Volo.Abp;

namespace Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView;

[GAgent]
public class WorkflowViewGAgent : GAgentBase<WorkflowViewState, WorkflowViewLogEvent, EventBase,
    WorkflowViewConfigDto>, IWorkflowViewGAgent
{
    private readonly IGAgentFactory _gAgentFactory;

    public WorkflowViewGAgent(IGAgentFactory agentFactory)
    {
        _gAgentFactory = agentFactory;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Workflow View GAgent");
    }

    [EventHandler]
    private async Task HandlerEventAsync(CreateWorkflowGEvent @event)
    {
        foreach (var workflowNode in State.WorkflowNodeList)
        {
            var agentId = workflowNode.AgentId == Guid.Empty ? Guid.NewGuid() : workflowNode.AgentId;
            var agentProperties =
                workflowNode.Properties.IsNullOrEmpty() ? string.Empty : JsonConvert.SerializeObject(workflowNode.Properties);
            await CreateOrUpdateBusinessAgent(agentId, workflowNode.AgentType, agentProperties);
            
            RaiseEvent(new UpdateNodeAgentIdLogEvent()
            {
                NodeId = workflowNode.NodeId,
                AgentId = agentId
            });
        }

        await ConfirmEvents();
        var workflowGAgentId = State.WorkflowCoordinatorGAgentId == Guid.Empty ? Guid.NewGuid() : State.WorkflowCoordinatorGAgentId;
        var workflowCoordinatorGAgent = GrainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowGAgentId);
        var workflowConfig = new WorkflowCoordinatorConfigDto();
        var nodeMap = State.WorkflowNodeList.ToDictionary(r => r.NodeId, r => r);
        foreach (var node in State.WorkflowNodeList)
        {
            var nodeUnitList = State.WorkflowNodeUnitList.Where(t => t.NodeId == node.NodeId).ToList();
            if (nodeUnitList.IsNullOrEmpty())
            {
                workflowConfig.WorkflowUnitList.Add(new WorkflowUnitDto()
                {
                    ExtendedData = node.ExtendedData,
                    GrainId = GrainId.Create(node.AgentType, GuidToGrainKey(node.AgentId)).ToString(),
                    NextGrainId = ""
                });
                continue;
            }
            foreach (var nodeUnit in nodeUnitList)
            {
                var nextNode = nodeMap[nodeUnit.NextNodeId];
                workflowConfig.WorkflowUnitList.Add(new WorkflowUnitDto()
                {
                    ExtendedData = node.ExtendedData,
                    GrainId = GrainId.Create(node.AgentType, GuidToGrainKey(node.AgentId)).ToString(),
                    NextGrainId = GrainId.Create(nextNode.AgentType, GuidToGrainKey(nextNode.AgentId)).ToString()
                });
            }
        }
        await workflowCoordinatorGAgent.ConfigAsync(workflowConfig);
        RaiseEvent(new UpdateWorkflowAgentIdLogEvent()
        {
            AgentId = workflowGAgentId
        });
    }
    
    private static string GuidToGrainKey(Guid primaryKey)
    {
        return primaryKey.ToString("N");
    }

    private async Task CreateOrUpdateBusinessAgent(Guid primaryKey, string agentType,
        string agentProperties)
    {
        var grainId = GrainId.Create(agentType, primaryKey.ToString("N"));
        var businessAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        
        var configurationType = await businessAgent.GetConfigurationTypeAsync();
        if (configurationType == null || configurationType.IsAbstract)
        {
            return;
        }

        if (agentProperties.IsNullOrEmpty())
        {
            return;
        }

        var config = JsonConvert.DeserializeObject(agentProperties, configurationType) as ConfigurationBase;
        if (config == null )
        {
            return;
        }

        await businessAgent.ConfigAsync(config);
    }
    
    protected override async Task PerformConfigAsync(WorkflowViewConfigDto configuration)
    {
        await TrySaveWorkflowViewAsync(configuration);
    }

    private async Task TrySaveWorkflowViewAsync(WorkflowViewConfigDto configuration)
    {
        if (configuration.WorkflowNodeList.IsNullOrEmpty() || configuration.Name.IsNullOrEmpty())
        {
            return;
        }

        foreach (var node in configuration.WorkflowNodeList)
        {
            if (node.NodeId == Guid.Empty || node.AgentType.IsNullOrEmpty() || node.Name.IsNullOrEmpty())
            {
                throw new ArgumentException("The workflow view node has invalid value.");
            }
        }

        var nodeIdList = configuration.WorkflowNodeList.Select(t => t.NodeId).ToList();
        var notExistedNodeId = configuration.WorkflowNodeUnitList.Where(t =>
            !nodeIdList.Contains(t.NodeId) || !nodeIdList.Contains(t.NextNodeId)).ToList();
        if (notExistedNodeId.Count > 0)
        {
            throw new ArgumentException("The workflow view invalid nodeId.");
        }

        var addNodeList = new List<WorkflowNodeDto>();
        var updateNodeList = new List<WorkflowNodeDto>();
        foreach (var node in configuration.WorkflowNodeList)
        {
            var stateNode = State.WorkflowNodeList.FirstOrDefault(t => t.NodeId == node.NodeId);
            if (stateNode == null)
            {
                addNodeList.Add(node);
                continue;
            }
            updateNodeList.Add(node);
        }

        var removeNodeIdList = State.WorkflowNodeList.Select(t => t.NodeId).Except(nodeIdList).ToList();
        
        RaiseEvent(new UpdateWorkflowViewLogEvent
        {
            AddNodeList = addNodeList,
            UpdateNodeList = updateNodeList,
            RemoveNodeIdList = removeNodeIdList,
            WorkflowNodeUnitList = configuration.WorkflowNodeUnitList,
            Name = configuration.Name
        });
    }

    protected override void GAgentTransitionState(WorkflowViewState state,
        StateLogEventBase<WorkflowViewLogEvent> @event)
    {
        switch (@event)
        {
            case UpdateWorkflowViewLogEvent updateWorkflowViewLogEvent:
                foreach (var removeNodeId in updateWorkflowViewLogEvent.RemoveNodeIdList)
                {
                    var removeNode = state.WorkflowNodeList.FirstOrDefault(t => t.NodeId == removeNodeId);
                    if (removeNode != null)
                    {
                        state.WorkflowNodeList.Remove(removeNode);
                    }
                }
                foreach (var node in updateWorkflowViewLogEvent.UpdateNodeList)
                {
                    var updateNode = state.WorkflowNodeList.FirstOrDefault(t => t.NodeId == node.NodeId);
                    if (updateNode != null)
                    {
                        updateNode.Name = node.Name;
                        updateNode.Properties = node.Properties;
                        updateNode.ExtendedData = node.ExtendedData;
                    }
                }
                state.WorkflowNodeList.AddRange(updateWorkflowViewLogEvent.AddNodeList);
                state.WorkflowNodeUnitList = updateWorkflowViewLogEvent.WorkflowNodeUnitList;
                state.Name = updateWorkflowViewLogEvent.Name;
                break;
            case UpdateNodeAgentIdLogEvent nodeAgentIdLogEvent:
                var updateAgentIdNode = state.WorkflowNodeList.FirstOrDefault(t => t.NodeId == nodeAgentIdLogEvent.NodeId);
                if (updateAgentIdNode != null)
                {
                    updateAgentIdNode.AgentId = nodeAgentIdLogEvent.AgentId;
                }
                break;
            case UpdateWorkflowAgentIdLogEvent updateWorkflowAgentIdLogEvent:
                state.WorkflowCoordinatorGAgentId = updateWorkflowAgentIdLogEvent.AgentId;
                break;
        }

        base.GAgentTransitionState(state, @event);
    }
}

public interface IWorkflowViewGAgent : IStateGAgent<WorkflowViewState>
{
}