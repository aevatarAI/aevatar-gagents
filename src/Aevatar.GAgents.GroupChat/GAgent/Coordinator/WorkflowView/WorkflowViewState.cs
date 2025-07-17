using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView;

[GenerateSerializer]
public class WorkflowViewState : StateBase
{
    [Id(0)] public List<WorkflowNodeInfo> WorkflowNodeList { get; set; } = new();
    [Id(1)] public List<WorkflowNodeUnitInfo> WorkflowNodeUnitList { get; set; } = new();
    [Id(2)] public string WorkflowCoordinatorGAgentGrainId { get; set; }
    [Id(3)] public string Name { get; set; }
}

[GenerateSerializer]
public class WorkflowNodeInfo
{
    [Id(0)] public string AgentType { get; set; }
    [Id(1)] public string Name { get; set; }
    [Id(2)] public Dictionary<string,string> ExtendedData { get; set; } = new();
    [Id(3)] public Dictionary<string, object>? Properties { get; set; } = new();
    [Id(4)] public Guid NodeId { get; set; }
    [Id(5)] public Guid AgentId { get; set; }
}

[GenerateSerializer]
public class WorkflowNodeUnitInfo
{
    [Id(0)] public Guid NodeId { get; set; }
    [Id(1)] public Guid NextNodeId { get; set; }
}