using System.Collections.Generic;
using System.Threading.Tasks;
using AutoGen.Core;
namespace Aevatar.GAgents.Autogen;

public interface IChatAgentProvider
{
    Task<IMessage?> SendAsync(string agentName, string message, IEnumerable<IMessage>? chatHistory);
    void SetAgent(string agentName, string systemMessage, FunctionCallMiddleware middleware);
}
