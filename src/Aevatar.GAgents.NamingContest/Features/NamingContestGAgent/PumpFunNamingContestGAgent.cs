using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.NamingContest.Common;
using Aevatar.GAgents.NamingContest.GEvents;
using Aevatar.GAgents.NamingContest.Grains;
using Aevatar.GAgents.NamingContest.VoteGAgent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Providers;

namespace AISmart.Agent;

[Description("Handle NamingContest")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class PumpFunPumpFunNamingContestGAgent : GAgentBase<PumpFunNamingContestGAgentState, PumpFunNameContestSEvent>, IPumpFunNamingContestGAgent
{
    private readonly ILogger<PumpFunPumpFunNamingContestGAgent> _logger;

    public PumpFunPumpFunNamingContestGAgent(ILogger<PumpFunPumpFunNamingContestGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    public Task<PumpFunPumpFunNamingContestGAgent> GetStateAsync()
    {
        throw new NotImplementedException();
    }

    public Task InitGroupInfoAsync(IniNetWorkMessagePumpFunSEvent iniNetWorkMessageSEvent)
    {
        RaiseEvent(iniNetWorkMessageSEvent);
        base.ConfirmEvents();
        return Task.CompletedTask;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for informing other agents when a PumpFun thread is published.");
    }
    

    [AllEventHandler]
    public async Task HandleRequestAllEventAsync(EventWrapperBase @event)
    {
        
        _logger.LogInformation("NamingContestGAgent HandleRequestAllEventAsync :" +
                               JsonConvert.SerializeObject(@event));
        var eventWrapper = @event as EventWrapper<EventBase>;

        if (eventWrapper?.Event != null)
        {
            await GrainFactory.GetGrain<INamingContestGrain>("NamingContestGrain")
                .SendMessageAsync(State.groupId,eventWrapper.Event as NamingAILogEvent, State.CallBackUrl);
        }
    }
    
    [EventHandler]
    public async Task HandleRequestEventAsync(VoteCharmingCompleteEvent @event)
    {
        
        _logger.LogInformation("NamingContestGAgent HandleRequestEventAsync VoteCharmingCompleteEvent:" +
                               JsonConvert.SerializeObject(@event));

        await GrainFactory.GetGrain<INamingContestGrain>("NamingContestGrain")
            .SendMessageAsync(State.groupId,@event, State.CallBackUrl);
    }
}

public interface IPumpFunNamingContestGAgent : IStateGAgent<PumpFunNamingContestGAgentState>
{ 
    Task InitGroupInfoAsync(IniNetWorkMessagePumpFunSEvent iniNetWorkMessageSEvent);
 
}