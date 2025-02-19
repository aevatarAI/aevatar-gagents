using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgent.NamingContest.Common;
using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.Basic.GroupGAgent;
using Aevatar.GAgents.Basic.PublishGAgent;
using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;
using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.HostAgent.Dto;
using AutoGen.Core;
using Microsoft.Extensions.Logging;

namespace AiSmart.GAgent.NamingContest.HostAgent;

[GAgent(nameof(HostGAgent))]
public class HostGAgent : GAgentBase<HostState, HostStateLogEvent, EventBase, InitHostDto>, IHostGAgent
{
    private readonly ILogger<HostGAgent> _logger;

    public HostGAgent(ILogger<HostGAgent> logger)
    {
        _logger = logger;
    }

    [EventHandler]
    public async Task HandleEventAsync(HostSummaryGEvent @event)
    {
        if (@event.HostId != this.GetPrimaryKey())
        {
            return;
        }

        var summaryReply = string.Empty;
        var prompt = NamingConstants.SummaryPrompt;
        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, @event.History);

            if (response != null && !response.Content.IsNullOrEmpty())
            {
                summaryReply = response.Content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Creative] TrafficInformCreativeGEvent error");
            summaryReply = NamingConstants.DefaultCreativeNaming;
        }
        finally
        {
            var groupGAgentId = @event.GroupId;
            var groupGAgent = GrainFactory.GetGrain<IGroupGAgent>(groupGAgentId);
            await groupGAgent.PublishEventAsync(new HostSummaryCompleteGEvent()
            {
                HostId = this.GetPrimaryKey(),
                SummaryReply = summaryReply,
                HostName = State.AgentName,
            });

            await groupGAgent.PublishEventAsync(new NamingAiLogGEvent(
                NamingContestStepEnum.HostSummary,
                this.GetPrimaryKey(),
                NamingRoleType.Host,
                State.AgentName,
                summaryReply,
                prompt));

            RaiseEvent(new AddHistoryChatStateLogEvent()
            {
                Message = new MicroAIMessage(Role.User.ToString(),
                    AssembleMessageUtil.AssembleNamingContent(State.AgentName, summaryReply))
            });

            await base.ConfirmEvents();
        }
    }

    public Task<MicroAIGAgentState> GetStateAsync()
    {
        throw new NotImplementedException();
    }
    
    public Task<HostState> GetGAgentState()
    {
        return Task.FromResult(State);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("the host agent");
    }

    public async Task SetAgentWithTemperatureAsync(string agentName, string agentResponsibility, float temperature,
        int? seed = null,
        int? maxTokens = null)
    {
        RaiseEvent(new SetAgentInfoStateLogEvent { AgentName = agentName, Description = agentResponsibility });
        await base.ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(agentName)
            .SetAgentWithTemperature(agentResponsibility, temperature, seed, maxTokens);
    }

    public Task<MicroAIGAgentState> GetAgentState()
    {
        throw new NotImplementedException();
    }

    public Task<string> GetCreativeNaming()
    {
        return Task.FromResult(State.Naming);
    }

    public Task<string> GetCreativeName()
    {
        return Task.FromResult(State.AgentName);
    }

    protected override async Task PerformConfigAsync(InitHostDto initializeDto)
    {
        RaiseEvent(new SetAgentInfoStateLogEvent
            { AgentName = initializeDto.AgentName, Description = initializeDto.AgentResponsibility });
        await base.ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(initializeDto.AgentName)
            .SetAgentAsync(initializeDto.AgentResponsibility);
    }
}