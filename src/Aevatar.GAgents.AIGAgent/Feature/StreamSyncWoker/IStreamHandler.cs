using System.Threading.Tasks;
using Orleans;

namespace Aevatar.AI.Feature.StreamSyncWoker;

public interface IStreamHandler<T> : IGrainWithGuidKey
{
    Task HandleStreamAsync(T arg);
}