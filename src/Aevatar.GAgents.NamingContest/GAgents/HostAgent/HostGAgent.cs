using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgent.NamingContest.Common;
using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.Basic.GroupGAgent;
using Aevatar.GAgents.Basic.PublishGAgent;
using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;
using Aevatar.GAgents.NamingContest.Common;
using AutoGen.Core;
using Microsoft.Extensions.Logging;

namespace AiSmart.GAgent.NamingContest.HostAgent;

public class HostGAgent : GAgentBase<HostState, HostStateLogEvent>, IHostGAgent
{
    private readonly ILogger<HostGAgent> _logger;

    public HostGAgent(ILogger<HostGAgent> logger) : base(logger)
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
            var groupGAgent = GrainFactory.GetGrain<GroupGAgent>(groupGAgentId);
            
            GrainId grainId = await groupGAgent.GetParentAsync();

            IPublishingGAgent publishingAgent;

            if (grainId != null && grainId.ToString().StartsWith("publishinggagent"))
            {
                publishingAgent = GrainFactory.GetGrain<IPublishingGAgent>(grainId);
            }
            else
            {
                publishingAgent = GrainFactory.GetGrain<IPublishingGAgent>(Guid.NewGuid());
                await publishingAgent.RegisterAsync(groupGAgent);
            }

            await publishingAgent.PublishEventAsync(new HostSummaryCompleteGEvent()
            {
                HostId = this.GetPrimaryKey(),
                SummaryReply = summaryReply,
                HostName = State.AgentName,
            });
            
            await publishingAgent.PublishEventAsync(new NamingAILogEvent(
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

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    public async Task SetAgent(string agentName, string agentResponsibility)
    {
        RaiseEvent(new SetAgentInfoStateLogEvent { AgentName = agentName, Description = agentResponsibility });
        await base.ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(agentName).SetAgentAsync(agentResponsibility);
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
}