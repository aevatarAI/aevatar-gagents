using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent.SEvents;
using Orleans;

namespace Aevatar.GAgents.MicroAI.GAgent;

public interface IChatAgentGrain : IGrainWithStringKey
{
    Task<MicroAIMessage?> SendAsync(string message, List<MicroAIMessage>? chatHistory);
    Task SendEventAsync(string message, List<MicroAIMessage>? chatHistory,object requestEvent);
    Task SetAgentAsync(string systemMessage);
    Task SetAgentWithRandomLLMAsync(string systemMessage);
    Task SetAgentAsync(string systemMessage,string llm);

    Task SetAgentWithTemperature(string systemMessage, float temperature, int? seed = null,
        int? maxTokens = null);
}