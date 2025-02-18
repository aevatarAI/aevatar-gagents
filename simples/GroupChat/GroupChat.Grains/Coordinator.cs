using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Coordinator;
using GroupChat.GAgent.Feature.Coordinator.LogEvent;
using Microsoft.Extensions.Logging;

namespace GroupChat.Grain;

[GenerateSerializer]
public class CoordinatorLogEvent : StateLogEventBase<CoordinatorLogEvent>
{
}

public class Coordinator : CoordinatorGAgentBase<CoordinatorStateBase, CoordinatorLogEvent>
{
    public Coordinator(ILogger<Coordinator> logger) : base(logger)
    {
    }

    protected override Task<bool> NeedCheckMemberInterestValue(List<GroupMember> members, Guid blackboardId)
    {
        return Task.FromResult(true);
    }
}