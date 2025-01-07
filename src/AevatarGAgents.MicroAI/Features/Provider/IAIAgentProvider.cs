using System.Collections.Generic;
using System.Threading.Tasks;
using AevatarGAgents.MicroAI.Agent.GEvents;
using AutoGen.Core;
using AutoGen.SemanticKernel;

namespace AevatarGAgents.MicroAI.Provider;

public interface IAIAgentProvider
{
    Task<MicroAIMessage?> SendAsync(MiddlewareStreamingAgent<SemanticKernelAgent> agent,string message, List<MicroAIMessage>? chatHistory);
    Task<MiddlewareStreamingAgent<SemanticKernelAgent>> GetAgentAsync(string agentName, string systemMessage);
}
