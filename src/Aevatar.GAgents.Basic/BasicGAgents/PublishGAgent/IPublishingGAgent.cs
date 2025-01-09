using System.Threading.Tasks;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Common.PublishGAgent;

public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}