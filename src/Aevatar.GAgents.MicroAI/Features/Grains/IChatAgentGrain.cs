using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Orleans;

namespace Aevatar.GAgents.MicroAI.Grains;

public interface IChatAgentGrain : IGrainWithStringKey
{
    Task<MicroAIMessage?> SendAsync(string message, List<MicroAIMessage>? chatHistory);
    Task SetAgentAsync(string systemMessage);

    Task SetAgentWithTemperature(string systemMessage, float temperature, int? seed = null,
        int? maxTokens = null);
}