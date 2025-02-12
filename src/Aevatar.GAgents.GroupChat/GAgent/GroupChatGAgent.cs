using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Abstract;
using Aevatar.GAgents.GroupChat.Features.Common;
using Aevatar.GAgents.GroupChat.Features.ExecuteAgent;
using Aevatar.GAgents.GroupChat.GEvent;
using Aevatar.GAgents.GroupChat.SEvent;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace Aevatar.GAgents.GroupChat.GAgent;

public class GroupChatGAgent<TInput> : GAgentBase<GroupChatState, GroupChatLogEventBase>, IPipelineGAgent
{
    public GroupChatGAgent(ILogger logger) : base(logger)
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
        var parentFullName = upstream.GetType().FullName;

        var childrenId = downstream.GetPrimaryKey();
        var childrenFullName = downstream.GetType().FullName;
        if (State.CheckHasUpstream(childrenId))
        {
            return false;
        }

        RaiseEvent(new OrchestrateJobLogEvent()
        {
            ParentId = parentId,
            ParentFullName = parentFullName,

            ChildrenId = childrenId,
            ChildrenFullName = childrenFullName,
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

        var stepType = Type.GetType(State.TopUpstream.FullName);
        if (stepType == null)
        {
            return false;
        }

        if (startMessage.GetType() != stepType)
        {
            return false;
        }

        List<GroupChatLogEventBase> events = new List<GroupChatLogEventBase>();
        events.Add(new StartGroupChatLogEvent());
        events.Add(new ActiveJobLogEvent() { JobId = State.TopUpstream.GrainId, InputParam = startMessage });
        RaiseEvents(events);

        await ConfirmEvents();

        await StartJob(State.TopUpstream.GrainId, State.TopUpstream.FullName, startMessage);

        return true;
    }

    private async Task StartJob(Guid grainId, string grainFullName, object input)
    {
        await PublishEventToExecutor(new JobExecuteInfo()
            { JobId = grainId, JobFullName = grainFullName, JobInputParam = input });
    }

    private async Task PublishEventToExecutor(JobExecuteInfo jobInfo)
    {
        var grain = GrainFactory.GetGrain<IExecuteJob>(Guid.NewGuid());
        await SubscribeStream(grain);
        _ = grain.ExecuteJobAsync(jobInfo);
    }

    private async Task SubscribeStream(IGrainWithGuidKey grain)
    {
        var streamId = StreamId.Create(CommonConstants.SteamProvider, grain.GetGrainId().ToString());
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
        List<GroupChatLogEventBase> events = new List<GroupChatLogEventBase>();
        events.Add(new JobFinishLogEvent() { JobId = executeResult.JobId });

        var jobInfo = State.GetJobInfo(executeResult.JobId);
        
        // do downstream job
        if (jobInfo != null && executeResult.IfContinue)
        {
            foreach (var childrenId in jobInfo.DownStreamList)
            {
                var childrenJob = State.GetJobInfo(childrenId);
                if (childrenJob == null) continue;
                
                await StartJob(childrenJob.GrainId, childrenJob.FullName, executeResult.ExecuteResponse);
                events.Add(new ActiveJobLogEvent(){JobId = childrenId, InputParam = executeResult.ExecuteResponse});
            }
        }

        RaiseEvents(events);
        await ConfirmEvents();

        // complete
        if (State.ActiveJobs.Count == 0)
        {
            RaiseEvent( new GroupChatCompleteLogEvent());
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