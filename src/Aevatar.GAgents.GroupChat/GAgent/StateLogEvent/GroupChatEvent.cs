using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.SEvent;

[GenerateSerializer]
public class GroupChatLogEventBase: StateLogEventBase <GroupChatLogEventBase>
{
    
}

[GenerateSerializer]
public class OrchestrateJobLogEvent : GroupChatLogEventBase
{
    public Guid ParentId { get; set; }
    public string ParentFullName { get; set; }
    
    public Guid ChildrenId { get; set; }
    public string ChildrenFullName { get; set; }
}

[GenerateSerializer]
public class ClearJobLogEvent : GroupChatLogEventBase
{
    
}

[GenerateSerializer]
public class ActiveJobLogEvent : GroupChatLogEventBase
{
    [Id(0)] public Guid JobId { get; set; }
    [Id(1)] public Object InputParam { get; set; }
}

[GenerateSerializer]
public class JobFinishLogEvent : GroupChatLogEventBase
{
    [Id(0)] public Guid JobId { get; set; }
}

[GenerateSerializer]
public class StartGroupChatLogEvent : GroupChatLogEventBase
{
    
}

[GenerateSerializer]
public class GroupChatCompleteLogEvent : GroupChatLogEventBase
{
    
} 