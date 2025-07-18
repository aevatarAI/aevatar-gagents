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
    [Id(2)] public string? InitContent { get; set; } = null;
    [Id(3)] public bool EnableExecutionRecord { get; set; }
}

[GenerateSerializer]
public class StartWorkUnitLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public long Term { get; set; }
}

[GenerateSerializer]
public class FinishedWorkUnitLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public long Term { get; set; }
}

[GenerateSerializer]
public class WorkflowFinishLogEvent : WorkflowCoordinatorLogEvent
{
    
}

[GenerateSerializer]
public class WorkflowStartLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public Guid ExecutionRecordId { get; set; }
}

[GenerateSerializer]
public class ResetWorkflowLogEvent : WorkflowCoordinatorLogEvent
{
}

[GenerateSerializer]
public class WorkflowStartFailedLogEvent : WorkflowCoordinatorLogEvent
{
    
}