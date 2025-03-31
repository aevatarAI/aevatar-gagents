using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

[GenerateSerializer]
public class WorkflowCoordinatorState : StateBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public int Term { get; set; } = 0;
    [Id(2)] public List<WorkUnitInfo> CurrentWorkUnitInfos { get; set; } = new List<WorkUnitInfo>();
    [Id(3)] public Dictionary<int, string> TermToWorkUnitGrainId { get; set; } = new Dictionary<int, string>();
    [Id(4)] public WorkflowCoordinatorStatus WorkflowStatus { get; set; } = WorkflowCoordinatorStatus.Pending;
    [Id(5)] public List<WorkUnitInfo> BackupWorkUnitInfos { get; set; } = new List<WorkUnitInfo>();

    public WorkUnitInfo? GetWorkUnit(string workUnitGrainId)
    {
        return CurrentWorkUnitInfos.FirstOrDefault(f => f.GrainId == workUnitGrainId);
    }

    public bool CheckAllWorkUnitFinished()
    {
        return CurrentWorkUnitInfos.Exists(
            e => e.UnitStatusEnum is WorkerUnitStatusEnum.Pending or WorkerUnitStatusEnum.InProgress) == false;
    }

    public bool CheckWorkUnitCanProgress(string workUnitGrainId)
    {
        var workUnitInfo = CurrentWorkUnitInfos.FirstOrDefault(f => f.GrainId == workUnitGrainId);
        if (workUnitInfo == null)
        {
            return false;
        }

        if (workUnitInfo.UnitStatusEnum != WorkerUnitStatusEnum.Pending)
        {
            return false;
        }

        var preWorkUnits = CurrentWorkUnitInfos.FindAll(f => f.NextGrainId == workUnitGrainId);
        return preWorkUnits.Exists(e => e.UnitStatusEnum != WorkerUnitStatusEnum.Finished) == false;
    }

    public List<string> GetUpStreamGrainIds(string currentGrainId)
    {
        return CurrentWorkUnitInfos.Where(w => w.NextGrainId == currentGrainId).Select(s => s.GrainId).ToList();
    }

    public List<string> GetTopUpStreamGrainIds()
    {
        var downStreamGrainIds = CurrentWorkUnitInfos.Where(w => !w.NextGrainId.IsNullOrEmpty()).Select(s => s.NextGrainId);
        return CurrentWorkUnitInfos.Where(w => downStreamGrainIds.Contains(w.GrainId) == false).Select(s => s.GrainId)
            .ToList();
    }

    public WorkUnitInfo? GetWorkUnitFromTerm(int termId)
    {
        return TermToWorkUnitGrainId.TryGetValue(termId, out var result) == false
            ? null
            : CurrentWorkUnitInfos.First(f => f.GrainId == result);
    }
}