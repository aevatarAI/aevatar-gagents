using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

[GenerateSerializer]
public class WorkflowCoordinatorState : StateBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public int Term { get; set; } = 0;
    [Id(2)] public List<WorkUnitInfo> WorkUnitInfos { get; set; } = new List<WorkUnitInfo>();
    [Id(3)] public Dictionary<int, string> TermToWorkUnitGrainId { get; set; } = new Dictionary<int, string>();
    [Id(4)] public WorkflowCoordinatorStatus WorkflowStatus { get; set; } = WorkflowCoordinatorStatus.Pending;

    public WorkUnitInfo? GetWorkUnit(string workUnitGrainId)
    {
        return WorkUnitInfos.FirstOrDefault(f => f.GrainId == workUnitGrainId);
    }

    public bool CheckAllWorkUnitFinished()
    {
        return WorkUnitInfos.Exists(
            e => e.UnitStatusEnum is WorkerUnitStatusEnum.Pending or WorkerUnitStatusEnum.InProgress) == false;
    }

    public bool CheckWorkUnitCanProgress(string workUnitGrainId)
    {
        var workUnitInfo = WorkUnitInfos.FirstOrDefault(f => f.GrainId == workUnitGrainId);
        if (workUnitInfo == null)
        {
            return false;
        }

        if (workUnitInfo.UnitStatusEnum != WorkerUnitStatusEnum.Pending)
        {
            return false;
        }

        var preWorkUnits = WorkUnitInfos.FindAll(f => f.NextGrainId == workUnitGrainId);
        return preWorkUnits.Exists(e => e.UnitStatusEnum != WorkerUnitStatusEnum.Finished) == false;
    }

    public List<string> GetUpStreamGrainIds(string currentGrainId)
    {
        return WorkUnitInfos.Where(w => w.NextGrainId == currentGrainId).Select(s => s.GrainId).ToList();
    }

    public List<string> GetTopUpStreamGrainIds()
    {
        var downStreamGrainIds = WorkUnitInfos.Where(w => !w.NextGrainId.IsNullOrEmpty()).Select(s => s.NextGrainId);
        return WorkUnitInfos.Where(w => downStreamGrainIds.Contains(w.GrainId) == false).Select(s => s.GrainId)
            .ToList();
    }

    public WorkUnitInfo? GetWorkUnitFromTerm(int termId)
    {
        return TermToWorkUnitGrainId.TryGetValue(termId, out var result) == false
            ? null
            : WorkUnitInfos.First(f => f.GrainId == result);
    }
}