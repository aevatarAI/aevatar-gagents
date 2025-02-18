using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.Abstractions;

namespace Aevatar.GAgents.Basic;

[GAgent]
public class TenantGAgent : GAgentBase<TenantGAgentState, TenantStateLogEvent, EventBase, TenantGAgentConfiguration>,
    ITenantGAgent
{
    private readonly IGAgentFactory _gAgentFactory;

    public TenantGAgent(IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for tenant.");
    }

    public Task<List<Guid>> GetTenantUserListAsync()
    {
        return Task.FromResult(State.TenantUserList);
    }

    protected override async Task OnRegisterAgentAsync(GrainId grainId)
    {
        await base.OnRegisterAgentAsync(grainId);
        // TODO: maybe Tenant manager should customize this.
        if (grainId.GetType() == typeof(UserGAgent))
        {
            RaiseEvent(new CreateUserStateLogEvent
            {
                UserId = grainId.GetGuidKey()
            });
        }
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(TenantGAgentState state,
        StateLogEventBase<TenantStateLogEvent> @event)
    {
        switch (@event)
        {
            case CreateUserStateLogEvent createUserEvent:
                State.TenantUserList.Add(createUserEvent.UserId);
                break;
            case SetTenantPolicyStateLogEvent setTenantPolicy:
                State.TenantPolicyGrainId = setTenantPolicy.TenantPolicyGrainId;
                break;
            case SetResourceMonitorStateLogEvent setResourceMonitor:
                State.ResourceMonitorPolicyGrainId = setResourceMonitor.ResourceMonitorGrainId;
                break;
        }

        base.GAgentTransitionState(state, @event);
    }

    protected override async Task PerformConfigAsync(TenantGAgentConfiguration configuration)
    {
        RaiseEvent(new SetTenantPolicyStateLogEvent
        {
            TenantPolicyGrainId = configuration.TenantPolicyGrainId
        });
        RaiseEvent(new SetResourceMonitorStateLogEvent
        {
            ResourceMonitorGrainId = configuration.ResourceMonitorPolicyGrainId
        });
        await ConfirmEvents();
        await base.PerformConfigAsync(configuration);
    }

    [GenerateSerializer]
    public class CreateUserStateLogEvent : TenantStateLogEvent
    {
        [Id(0)] public Guid UserId { get; set; }
    }

    [GenerateSerializer]
    public class SetTenantPolicyStateLogEvent : TenantStateLogEvent
    {
        [Id(0)] public GrainId TenantPolicyGrainId { get; set; }
    }

    [GenerateSerializer]
    public class SetResourceMonitorStateLogEvent : TenantStateLogEvent
    {
        [Id(0)] public GrainId ResourceMonitorGrainId { get; set; }
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // TODO: Move this code to tenant startup logic.
        // var tenantPolicyGAgent = await _gAgentFactory.GetGAgentAsync<ITenantPolicyGAgent>(this.GetPrimaryKey());
        // await RegisterAsync(tenantPolicyGAgent);
        await base.OnGAgentActivateAsync(cancellationToken);
    }
}