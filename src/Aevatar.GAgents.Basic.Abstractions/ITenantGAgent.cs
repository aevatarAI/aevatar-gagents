using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.Abstractions;

public interface ITenantGAgent : IGAgent
{
    Task<List<Guid>> GetTenantUserListAsync();
}

[GenerateSerializer]
public class TenantGAgentState : StateBase
{
    [Id(0)] public List<Guid> TenantUserList { get; set; } = [];
    [Id(1)] public GrainId TenantPolicyGrainId { get; set; }
    [Id(2)] public GrainId ResourceMonitorPolicyGrainId { get; set; }
}

[GenerateSerializer]
public class TenantStateLogEvent : StateLogEventBase<TenantStateLogEvent>;

[GenerateSerializer]
public class TenantGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public GrainId TenantPolicyGrainId { get; set; }
    [Id(1)] public GrainId ResourceMonitorPolicyGrainId { get; set; }
}