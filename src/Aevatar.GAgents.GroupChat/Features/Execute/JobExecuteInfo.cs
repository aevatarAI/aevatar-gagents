namespace Aevatar.GAgents.GroupChat.Features.ExecuteAgent;

[GenerateSerializer]
public class JobExecuteBase
{
}

[GenerateSerializer]
public class JobExecuteInfo : JobExecuteBase
{
    [Id(0)] public Guid JobId { get; set; }
    [Id(1)] public string JobFullName { get; set; }
    [Id(2)] public object JobInputParam { get; set; }
}

[GenerateSerializer]
public class JobExecuteResultInfo : JobExecuteBase
{
    [Id(0)] public Guid JobId { get; set; }
    [Id(1)] public bool IfContinue { get; set; }
    [Id(2)] public object? ExecuteResponse { get; set; }
}