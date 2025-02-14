using GroupChat.GAgent.Feature.Coordinator;
using GroupChat.GAgent.Feature.Coordinator.LogEvent;
using Microsoft.Extensions.Logging;

namespace GroupChat.Grain;

public class Coordinator:CoordinatorGAgentBase
{
    public Coordinator(ILogger<Coordinator> logger) : base(logger)
    {
    }

    protected override Task<bool> NeedCheckMemberInterestValue(List<GroupMember> members, Guid blackboardId)
    {
        return Task.FromResult(true);
    }
}