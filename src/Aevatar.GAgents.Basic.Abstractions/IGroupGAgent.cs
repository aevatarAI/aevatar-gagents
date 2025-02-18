using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.Abstractions;

[Obsolete("This interface is obsolete. Use IRelayGAgent instead.")]
public interface IGroupGAgent: IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}

[GenerateSerializer]
public class GroupGAgentState : StateBase
{
    [Id(0)] public int RegisteredGAgents { get; set; }
}

[GenerateSerializer]
public class GroupStateLogEvent : StateLogEventBase<GroupStateLogEvent>;
