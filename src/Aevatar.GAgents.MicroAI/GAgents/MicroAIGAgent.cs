
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.MicroAI.Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Aevatar.GAgents.MicroAI.Agent;

[Description("micro AI")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class MicroAIGAgent : GAgentBase<MicroAIGAgentState, AIMessageGEvent>, IMicroAIGAgent
{
    protected readonly ILogger<MicroAIGAgent> _logger;

    public MicroAIGAgent(ILogger<MicroAIGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for informing other agents when a micro AI thread is published.");
    }


    public async Task SetAgent(string agentName, string agentResponsibility)
    {
        RaiseEvent(new AISetAgentMessageGEvent
        {
            AgentName = agentName,
            AgentResponsibility = agentResponsibility
        });
        await ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(agentName).SetAgentAsync(agentResponsibility);
    }

    public async Task SetAgentWithTemperatureAsync(string agentName, string agentResponsibility, float temperature,
        int? seed = null,
        int? maxTokens = null)
    {
        RaiseEvent(new AISetAgentMessageGEvent
        {
            AgentName = agentName,
            AgentResponsibility = agentResponsibility
        });
        await ConfirmEvents();
        await GrainFactory.GetGrain<IChatAgentGrain>(agentName)
            .SetAgentWithTemperature(agentResponsibility, temperature, seed, maxTokens);
    }

    public async Task<MicroAIGAgentState> GetAgentState()
    {
        return State;
    }
}

public interface IMicroAIGAgent : IStateGAgent<MicroAIGAgentState>
{
    Task SetAgent(string agentName, string agentResponsibility);

    Task SetAgentWithTemperatureAsync(string agentName, string agentResponsibility, float temperature, int? seed = null,
        int? maxTokens = null);

    Task<MicroAIGAgentState> GetAgentState();
}