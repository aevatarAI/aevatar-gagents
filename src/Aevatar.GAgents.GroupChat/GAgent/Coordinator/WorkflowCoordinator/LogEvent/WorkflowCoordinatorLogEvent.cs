using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator.LogEvent;

[GenerateSerializer]
public class WorkflowCoordinatorLogEvent : StateLogEventBase<WorkflowCoordinatorLogEvent>
{
    
}

[GenerateSerializer]
public class SetWorkflowCoordinatorLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public List<WorkflowUnitDto> WorkflowUnit { get; set; } = new();
    [Id(1)] public Guid BlackBoardId { get; set; }
}

[GenerateSerializer]
public class StartWorkUnitLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public int Term { get; set; }
}

[GenerateSerializer]
public class FinishedWorkUnitLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public int Term { get; set; }
}

[GenerateSerializer]
public class WorkflowFinishLogEvent : WorkflowCoordinatorLogEvent
{
    
}

[GenerateSerializer]
public class WorkflowStartLogEvent : WorkflowCoordinatorLogEvent
{
    
}

[GenerateSerializer]
public class ResetWorkflowLogEvent : WorkflowCoordinatorLogEvent
{
}