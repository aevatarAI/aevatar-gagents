using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.Abstractions;

public interface IDatabaseGAgent : IGAgent
{
    Task StoreAsync<T>(T data) where T : class;
    Task<T> RetrieveAsync<T>(string id) where T : class;
}