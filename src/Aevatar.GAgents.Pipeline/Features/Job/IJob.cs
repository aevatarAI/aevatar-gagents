using Orleans;

namespace Aevatar.GAgents.Pipeline.Abstract;

public interface IJob<in TParam, TResult> : IGrainWithGuidKey
{
    Task<JobProgressResult<TResult>> ProcessAsync(TParam param);
}