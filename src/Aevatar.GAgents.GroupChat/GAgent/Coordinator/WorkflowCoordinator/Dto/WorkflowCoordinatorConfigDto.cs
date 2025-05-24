using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;

[GenerateSerializer]
public class WorkflowCoordinatorConfigDto:ConfigurationBase
{
    [Id(0)] public List<WorkflowUnitDto> WorkflowUnitList { get; set; } = new();
}

[GenerateSerializer]
public class WorkflowUnitDto
{
    [Id(0)] public string GrainId { get; set; }
    [Id(1)] public string NextGrainId { get; set; }
    [Id(2)] public Dictionary<string,string> ExtendedData { get; set; } = new();
}