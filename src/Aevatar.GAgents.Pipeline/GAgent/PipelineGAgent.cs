using System.Reflection;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Pipeline.Abstract;
using Aevatar.GAgents.Pipeline.Features.Common;
using Aevatar.GAgents.Pipeline.Features.ExecuteAgent;
using Aevatar.GAgents.Pipeline.GEvent;
using Aevatar.GAgents.Pipeline.SEvent;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace Aevatar.GAgents.Pipeline.GAgent;

public class PipelineGAgent : GAgentBase<GroupChatState, PipelineLogEventBase>, IPipelineGAgent
{
    public PipelineGAgent(ILogger<PipelineGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("this is pipeline group agent");
    }

    public async Task<bool> OrchestrateJobAsync<TIn, TPro, TOut>(IJob<TIn, TPro> upstream, IJob<TPro, TOut> downstream)
    {
        if (State.ExecuteStatus == ExecuteStatus.Progress)
        {
            return false;
        }

        var parentId = upstream.GetPrimaryKey();

        var childrenId = downstream.GetPrimaryKey();
        if (State.CheckHasUpstream(childrenId))
        {
            return false;
        }

        RaiseEvent(new OrchestrateJobLogEvent()
        {
            ParentId = parentId,
            ParentType = upstream.GetType(),

            ChildrenId = childrenId,
            ChildrenType = downstream.GetType(),
        });

        await ConfirmEvents();

        return true;
    }

    public async Task<bool> ClearAllJobAsync()
    {
        if (State.ExecuteStatus == ExecuteStatus.Progress)
        {
            return false;
        }

        RaiseEvent(new ClearJobLogEvent());
        await ConfirmEvents();
        return true;
    }

    public async Task<bool> StartAsync(object startMessage)
    {
        if (State.TopUpstream == null)
        {
            return false;
        }

        if (State.ExecuteStatus == ExecuteStatus.Progress)
        {
            return false;
        }

        var stepType = State.TopUpstream.JobType;

        if (stepType == null)
        {
            return false;
        }

        var jobObj = stepType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJob<,>));
        if (jobObj == null)
        {
            return false;
        }

        var genericArguments = jobObj.GetGenericArguments();
        if (startMessage.GetType() != genericArguments[0])
        {
            return false;
        }

        List<PipelineLogEventBase> events = new List<PipelineLogEventBase>();
        events.Add(new StartPipelineLogEvent());
        events.Add(new ActiveJobLogEvent() { JobId = State.TopUpstream.GrainId, InputParam = startMessage });
        RaiseEvents(events);

        await ConfirmEvents();

        await StartJob(State.TopUpstream.GrainId, State.TopUpstream.JobType, startMessage);

        return true;
    }

    private async Task StartJob(Guid grainId, Type jobType, object input)
    {
        await PublishEventToExecutor(new JobExecuteInfo()
            { JobId = grainId, JobType = jobType, JobInputParam = input });
    }

    private async Task PublishEventToExecutor(JobExecuteInfo jobInfo)
    {
        var grain = GrainFactory.GetGrain<IExecuteJob>(Guid.NewGuid());
        await SubscribeStream(grain);
        _ = grain.ExecuteJobAsync(jobInfo);
    }

    private async Task SubscribeStream(IExecuteJob grain)
    {
        var streamId = StreamId.Create(CommonConstants.SteamProvider, grain.GetPrimaryKey());
        var stream = StreamProvider.GetStream<JobExecuteBase>(streamId);
        await stream.SubscribeAsync(async (message, token) =>
        {
            // handle job complete 
            if (message is JobExecuteResultInfo @event)
            {
                await JobCompleteHandler(@event);
            }
        });
    }

    private async Task JobCompleteHandler(JobExecuteResultInfo executeResult)
    {
        List<PipelineLogEventBase> events = new List<PipelineLogEventBase>();
        events.Add(new JobFinishLogEvent() { JobId = executeResult.JobId });

        var jobInfo = State.GetJobInfo(executeResult.JobId);

        // do downstream job
        if (jobInfo != null && executeResult.IfContinue)
        {
            foreach (var childrenId in jobInfo.DownStreamList)
            {
                var childrenJob = State.GetJobInfo(childrenId);
                if (childrenJob == null) continue;

                await StartJob(childrenJob.GrainId, childrenJob.JobType, executeResult.ExecuteResponse);
                events.Add(new ActiveJobLogEvent() { JobId = childrenId, InputParam = executeResult.ExecuteResponse });
            }
        }

        RaiseEvents(events);
        await ConfirmEvents();

        // complete
        if (State.ActiveJobs.Count == 0)
        {
            RaiseEvent(new PipelineCompleteLogEvent());
            await ConfirmEvents();
        }
    }
}

public interface IPipelineGAgent : IGrainWithGuidKey
{
    Task<bool> OrchestrateJobAsync<TIn, TPro, TOut>(IJob<TIn, TPro> upstream, IJob<TPro, TOut> downstream);
    Task<bool> ClearAllJobAsync();
    Task<bool> StartAsync(object startMessage);
}