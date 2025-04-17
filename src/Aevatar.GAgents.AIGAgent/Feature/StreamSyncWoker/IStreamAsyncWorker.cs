using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.SyncWork;

namespace Aevatar.AI.Feature.StreamSyncWoker;

public interface IStreamAsyncWorker<TaskRequest, TResponse> : ISyncWorker<TaskRequest, TResponse>, IGrainWithGuidKey
{
    Task SetLongRunTaskAsync(GrainId grainId);
}