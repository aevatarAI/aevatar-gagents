using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.Abstractions;

// Use belonger

public interface ITenantPolicyGAgent : IGAgent
{
    Task<List<GrainType>> GetGetEditableGrainTypesAsync(Guid userId);
}

[GenerateSerializer]
public class TenantPolicyGAgentState : StateBase
{

}

[GenerateSerializer]
public class TenantPolicyStateLogEvent : StateLogEventBase<TenantPolicyStateLogEvent>
{

}