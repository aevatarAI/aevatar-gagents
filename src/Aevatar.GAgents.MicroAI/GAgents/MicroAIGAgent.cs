

using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Model;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.GAgents.MicroAI.GAgent;

[Description("micro AI")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class MicroAIGAgent : GAgentBase<MicroAIGAgentState, AIMessageStateLogEvent>, IMicroAIGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for informing other agents when a micro AI thread is published.");
    }


    public async Task SetAgent(string agentName, string agentResponsibility)
    {
        RaiseEvent(new AISetAgentStateLogEvent
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
        RaiseEvent(new AISetAgentStateLogEvent
        {
            AgentName = agentName,
            AgentResponsibility = agentResponsibility
        });
        await ConfirmEvents();
        
        State.AgentName = agentName;
        State.AgentResponsibility = agentResponsibility;
        
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