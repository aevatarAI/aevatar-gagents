using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;

[GenerateSerializer]
public class WorkflowCoordinatorConfigDto:ConfigurationBase
{
    [Id(0)] public List<WorkflowUnitDto> WorkflowUnitList { get; set; }
    [Id(1)] public Guid BlackBoardId { get; set; }
}

[GenerateSerializer]
public class WorkflowUnitDto
{
    [Id(0)] public string GrainId { get; set; }
    [Id(1)] public string NextGrainId { get; set; }
}