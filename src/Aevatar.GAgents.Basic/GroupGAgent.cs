using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Basic;

[GAgent, Obsolete("This agent is obsolete. Use RelayGAgent instead.")]
public class GroupGAgent : GAgentBase<GroupGAgentState, GroupStateLogEvent>, IGroupGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("An agent to inform other agents when a social event is published.");
    }

    public async Task PublishEventAsync<T>(T @event) where T : EventBase
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        await PublishAsync(@event);
    }

    protected override Task OnRegisterAgentAsync(GrainId grainId)
    {
        ++State.RegisteredGAgents;
        return Task.CompletedTask;
    }

    protected override Task OnUnregisterAgentAsync(GrainId grainId)
    {
        --State.RegisteredGAgents;
        return Task.CompletedTask;
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        State.RegisteredGAgents = 0;
    }
}