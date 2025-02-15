using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Pipeline.SEvent;

[GenerateSerializer]
public class PipelineLogEventBase : StateLogEventBase<PipelineLogEventBase>
{
}

[GenerateSerializer]
public class OrchestrateJobLogEvent : PipelineLogEventBase
{
    [Id(0)] public Guid ParentId { get; set; }
    [Id(1)] public Type ParentType { get; set; }

    [Id(2)] public Guid ChildrenId { get; set; }
    [Id(3)] public Type ChildrenType { get; set; }
}

[GenerateSerializer]
public class ClearJobLogEvent : PipelineLogEventBase
{
}

[GenerateSerializer]
public class ActiveJobLogEvent : PipelineLogEventBase
{
    [Id(0)] public Guid JobId { get; set; }
    [Id(1)] public Object InputParam { get; set; }
}

[GenerateSerializer]
public class JobFinishLogEvent : PipelineLogEventBase
{
    [Id(0)] public Guid JobId { get; set; }
}

[GenerateSerializer]
public class StartPipelineLogEvent : PipelineLogEventBase
{
}

[GenerateSerializer]
public class PipelineCompleteLogEvent : PipelineLogEventBase
{
}