using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.NamingContest.Common;
using Aevatar.GAgents.NamingContest.Features.PumpfunGrain;
using Aevatar.GAgents.NamingContest.GAgents.LogEventAgent.Dto;
using Aevatar.GAgents.NamingContest.GAgents.LogEventAgent.GEvent;
using Aevatar.GAgents.NamingContest.GAgents.LogEventAgent.StateLogEvent;
using AiSmart.GAgent.NamingContest.JudgeAgent;
using AiSmart.GAgent.NamingContest.JudgeAgent.Dto;
using AiSmart.GAgent.NamingContest.VoteAgent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.GAgents.NamingContest.GAgents.LogEventAgent;

[GAgent(nameof(LogEventGAgent))]
public class LogEventGAgent : GAgentBase<LogEventState, LogEventStateEvent, EventBase, InitLogEvent>, ILogEventGAgent
{
    public LogEventGAgent(ILogger logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("the logEvent agent");
    }

    public override async Task InitializeAsync(InitLogEvent initializeDto)
    {
        RaiseEvent(new InitLogEventState()
        {
            CallBackUrl = initializeDto.CallBackUrl,
            Round = initializeDto.Round,
            GroupId = initializeDto.GroupId,
            MostCharmingBackUrl = initializeDto.MostCharmingBackUrl,
            MostCharmingGroupId = initializeDto.MostCharmingGroupId,
        });
        
        await ConfirmEvents();
    }
    
    [AllEventHandler]
    public async Task HandleRequestAllEventAsync(EventWrapperBase @event)
    {
        var eventWrapper = @event as EventWrapper<EventBase>;
        if (eventWrapper?.Event != null)
        {
            if (eventWrapper.Event is NamingAiLogGEvent logEvent)
            {
                await GrainFactory.GetGrain<INamingContestGrain>("NamingContestGrain")
                    .SendMessageAsync(State.GroupId, logEvent, State.CallBackUrl);
            }
            else if (eventWrapper.Event is NamingLogGEvent namingLogEvent)
            {
                await GrainFactory.GetGrain<INamingContestGrain>("NamingContestGrain")
                    .SendMessageAsync(State.GroupId, namingLogEvent, State.CallBackUrl);
            }
        }
    }

    [EventHandler]
    public async Task HandleRequestEventAsync(VoteCharmingCompleteEvent @event)
    {
        Logger.LogInformation("NamingContestGAgent HandleRequestEventAsync VoteCharmingCompleteEvent:" +
                               JsonConvert.SerializeObject(@event));

        await GrainFactory.GetGrain<INamingContestGrain>("NamingContestGrain")
            .SendMessageAsync(State.MostCharmingGroupId, @event, State.MostCharmingBackUrl);
    }
}

public interface ILogEventGAgent : IGrainWithGuidKey
{
}