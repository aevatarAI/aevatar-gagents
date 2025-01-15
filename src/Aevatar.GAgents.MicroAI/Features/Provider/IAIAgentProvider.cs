using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.MicroAI.Agent.SEvents;
using AutoGen.Core;
using AutoGen.SemanticKernel;

namespace Aevatar.GAgents.MicroAI.Provider;

public interface IAIAgentProvider
{
    Task<MicroAIMessage?> SendAsync(MiddlewareStreamingAgent<SemanticKernelAgent> agent,string message, List<MicroAIMessage>? chatHistory);
    Task<MiddlewareStreamingAgent<SemanticKernelAgent>> GetAgentAsync(string agentName, string systemMessage);
}
