using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Pipeline.Abstract;

public interface IJob<in TParam, TResult> : IGAgent
{
    Task<JobProgressResult<TResult>> ProcessAsync(TParam param);
}