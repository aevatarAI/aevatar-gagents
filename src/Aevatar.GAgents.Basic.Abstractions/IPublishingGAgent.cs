using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.Abstractions;

public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}

[GenerateSerializer]
public class PublishingAgentState : StateBase;

[GenerateSerializer]
public class PublishingStateLogEvent : StateLogEventBase<PublishingStateLogEvent>;
