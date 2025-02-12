using Orleans;

namespace Aevatar.GAgents.GroupChat.Abstract;

public interface IJob<in TParam, TResult> : IGrainWithGuidKey
{
    Task<JobProgressResult<TResult>> ProcessAsync(TParam param);
}