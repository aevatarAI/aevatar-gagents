using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Pipeline.SEvent;

namespace Aevatar.GAgents.Pipeline.GEvent;

[GenerateSerializer]
public class GroupChatState : StateBase
{
    [Id(0)] public ExecuteStatus ExecuteStatus { get; set; } = ExecuteStatus.WaitForStart;
    [Id(1)] public JobInfo? TopUpstream { get; set; } = null;
    [Id(2)] public Dictionary<Guid, JobInfo> JobInfos { get; set; } = new Dictionary<Guid, JobInfo>();
    [Id(3)] public Dictionary<Guid, Guid> Downstream2Upstream { get; set; } = new Dictionary<Guid, Guid>();
    [Id(4)] public Dictionary<Guid, ActiveJob> ActiveJobs { get; set; } = new Dictionary<Guid, ActiveJob>();
    
    public void Apply(OrchestrateJobLogEvent @event)
    {
        if (JobInfos.TryGetValue(@event.ParentId, out var parentJob) == false)
        {
            parentJob = new JobInfo(@event.ParentFullName, @event.ParentId);
            JobInfos.Add(@event.ParentId, parentJob);
        }

        TopUpstream ??= parentJob;

        parentJob.AddChildren(@event.ChildrenId);
        if (JobInfos.TryGetValue(@event.ChildrenId, out var childrenJob) == false)
        {
            childrenJob = new JobInfo(@event.ChildrenFullName, @event.ChildrenId);
            JobInfos.Add(@event.ChildrenId, childrenJob);
        }

        if (childrenJob.GrainId == TopUpstream.GrainId)
        {
            TopUpstream = parentJob;
        }

        Downstream2Upstream.Add(@event.ChildrenId, @event.ParentId);
    }

    public void Apply(ClearJobLogEvent @event)
    {
        TopUpstream = null;
        JobInfos.Clear();
        Downstream2Upstream.Clear();
    }

    public void Apply(ActiveJobLogEvent @event)
    {
        var activeJob = new ActiveJob() { JobId = @event.JobId, JobInput = @event.InputParam };
        ActiveJobs.TryAdd(@event.JobId, activeJob);
    }

    public void Apply(JobFinishLogEvent @event)
    {
        ActiveJobs.Remove(@event.JobId);
    }

    public void Apply(StartPipelineLogEvent @event)
    {
        ExecuteStatus = ExecuteStatus.Progress;
    }

    public void Apply(PipelineCompleteLogEvent @event)
    {
        ExecuteStatus = ExecuteStatus.WaitForStart;
    }
    
    public JobInfo? GetJobInfo(Guid jobId)
    {
        JobInfos.TryGetValue(jobId, out var result);
        return result;
    }
    
    public bool CheckHasUpstream(Guid downstreamId)
    {
        return Downstream2Upstream.ContainsKey(downstreamId);
    }
}

[GenerateSerializer]
public class JobInfo
{
    [Id(0)] public string FullName { get; set; }
    [Id(1)] public Guid GrainId { get; set; }
    [Id(3)] public List<Guid> DownStreamList { get; set; }

    public JobInfo(string fullName, Guid grainId)
    {
        FullName = fullName;
        GrainId = grainId;
        DownStreamList = new List<Guid>();
    }

    public void AddChildren(Guid children)
    {
        if (DownStreamList.Contains(children) == false)
        {
            DownStreamList.Add(children);
        }
    }
}

[GenerateSerializer]
public class ActiveJob
{
    [Id(0)] public Guid JobId { get; set; }
    [Id(1)] public Object JobInput { get; set; }
}

[GenerateSerializer]
public enum ExecuteStatus
{
    WaitForStart,
    Progress,
}