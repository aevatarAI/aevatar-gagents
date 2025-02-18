using System.Reflection;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aevatar.GAgents.Basic;

[GAgent]
public class RelayGAgent : GAgentBase<RelayGAgentState, RelayStateLogEvent, EventBase, RelayGAgentConfiguration>,
    IRelayGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for relaying events.");
    }

    public Task<List<Type>> GetUpwardsEventTypesAsync()
    {
        return Task.FromResult(State.UpwardsEventTypes);
    }

    public Task<List<Type>> GetNotDownwardsEventTypesAsync()
    {
        return Task.FromResult(State.NotDownwardsEventTypes);
    }

    protected override async Task PerformConfigAsync(RelayGAgentConfiguration configuration)
    {
        RaiseEvent(new AddUpwardsEventTypes
        {
            TypeList = configuration.UpwardsEventTypes
        });
        RaiseEvent(new AddNotDownwardsEventTypes
        {
            TypeList = configuration.NotDownwardsEventTypes
        });
        await ConfirmEvents();
    }

    [AllEventHandler]
    public async Task UpwardsAsync(EventWrapperBase eventWrapperBase)
    {
        var parent = State.Parent;
        if (parent == null) return;
        var eventWrapper = (EventWrapper<EventBase>)eventWrapperBase;
        if (State.UpwardsEventTypes.Contains(eventWrapper.Event.GetType()))
        {
            await GetStream(parent.Value.ToString()).OnNextAsync(eventWrapper);
        }
    }
    
    private IAsyncStream<EventWrapperBase> GetStream(string grainIdString)
    {
        var streamId = StreamId.Create(AevatarCoreConstants.StreamNamespace, grainIdString);
        return StreamProvider.GetStream<EventWrapperBase>(streamId);
    }

    [GenerateSerializer]
    public class AddUpwardsEventTypes : RelayStateLogEvent
    {
        [Id(0)] public List<Type> TypeList { get; set; } = [];
    }

    [GenerateSerializer]
    public class AddNotDownwardsEventTypes : RelayStateLogEvent
    {
        [Id(0)] public List<Type> TypeList { get; set; } = [];
    }
}