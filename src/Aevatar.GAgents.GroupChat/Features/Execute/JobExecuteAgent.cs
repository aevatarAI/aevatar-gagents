using Aevatar.GAgents.GroupChat.Abstract;
using Aevatar.GAgents.GroupChat.Features.Common;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aevatar.GAgents.GroupChat.Features.ExecuteAgent;

internal class JobExecuteAgent : Grain, IExecuteJob
{
    private IStreamProvider StreamProvider => this.GetStreamProvider(CommonConstants.SteamProvider);
    private readonly ILogger<JobExecuteAgent> _logger;

    public JobExecuteAgent(ILogger<JobExecuteAgent> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteJobAsync(JobExecuteInfo jobExecuteInfo)
    {
        bool ifContinue = false;
        object? response = null;
        try
        {
            var jobType = Type.GetType(jobExecuteInfo.JobFullName);
            if (jobType != null)
            {
                var stepGrain = GrainFactory.GetGrain(jobType, jobExecuteInfo.JobId);
                if (stepGrain != null)
                {
                    var processMethod = jobType.GetMethod("ProcessAsync");
                    if (processMethod != null)
                    {
                        var executeTask = processMethod.Invoke(stepGrain,
                            new[] { jobExecuteInfo.JobInputParam });
                        if (executeTask != null)
                        {
                            var result = await (Task<JobProgressResult<object>>)executeTask;
                            ifContinue = result.IfContinue;
                            response = result.Result;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pipeline] JobExecuteAgent ExecuteJobAsync error");
        }

        await PublishInternalEvent(new JobExecuteResultInfo()
            { JobId = jobExecuteInfo.JobId, IfContinue = ifContinue, ExecuteResponse = response });
    }

    private async Task PublishInternalEvent(JobExecuteResultInfo publishData)
    {
        var streamId = StreamId.Create(CommonConstants.SteamProvider, this.GetGrainId().ToString());
        var stream = StreamProvider.GetStream<JobExecuteResultInfo>(streamId);
        await stream.OnNextAsync(publishData);
    }
}

internal interface IExecuteJob : IGrainWithGuidKey
{
    Task ExecuteJobAsync(JobExecuteInfo jobExecuteInfo);
}