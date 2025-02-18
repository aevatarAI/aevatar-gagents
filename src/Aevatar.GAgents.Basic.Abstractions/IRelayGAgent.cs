using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.Abstractions;

public interface IRelayGAgent : IGAgent
{
    Task<List<Type>> GetUpwardsEventTypesAsync();
    Task<List<Type>> GetNotDownwardsEventTypesAsync();
}


[GenerateSerializer]
public class RelayGAgentState : StateBase
{
    [Id(0)] public List<Type> UpwardsEventTypes { get; set; } = [];
    [Id(1)] public List<Type> NotDownwardsEventTypes { get; set; } = [];
}

[GenerateSerializer]
public class RelayStateLogEvent : StateLogEventBase<RelayStateLogEvent>;

[GenerateSerializer]
public class RelayGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public List<Type> UpwardsEventTypes { get; set; } = [];
    [Id(1)] public List<Type> NotDownwardsEventTypes { get; set; } = [];
}