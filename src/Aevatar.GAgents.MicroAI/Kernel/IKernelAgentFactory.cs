using AutoGen.Core;
using AutoGen.SemanticKernel;

namespace Aevatar.GAgents.MicroAI;

public interface IKernelAgentFactory
{
    MiddlewareStreamingAgent<SemanticKernelAgent> CreateAgent(string systemName, string llmType, string systemMessage);
}