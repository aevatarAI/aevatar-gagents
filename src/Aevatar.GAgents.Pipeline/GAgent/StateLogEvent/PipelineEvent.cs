using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Pipeline.SEvent;

[GenerateSerializer]
public class PipelineLogEventBase: StateLogEventBase <PipelineLogEventBase>
{
    
}

[GenerateSerializer]
public class OrchestrateJobLogEvent : PipelineLogEventBase
{
    public Guid ParentId { get; set; }
    public string ParentFullName { get; set; }
    
    public Guid ChildrenId { get; set; }
    public string ChildrenFullName { get; set; }
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