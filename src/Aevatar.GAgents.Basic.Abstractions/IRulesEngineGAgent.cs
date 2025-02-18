using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.Abstractions;

public interface IRulesEngineGAgent : IGAgent
{
    Task ExecuteAsync(dynamic data);
}