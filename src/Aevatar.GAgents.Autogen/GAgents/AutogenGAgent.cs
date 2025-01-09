using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Autogen.Common;
using Aevatar.GAgents.Autogen.DescriptionManager;
using Aevatar.GAgents.Autogen.Events.Dto;
using Aevatar.GAgents.Autogen.Events.InternalEvents;
using Aevatar.GAgents.Autogen.EventSourcingEvent;
using Aevatar.GAgents.Autogen.Executor;
using Aevatar.GAgents.Autogen.State;
using Aevatar.GAgents.Rag;
using AutoGen.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;
using IStreamProvider = Orleans.Streams.IStreamProvider;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Aevatar.GAgents.Autogen;


public interface IAutogenGAgent : IGAgent
{
    Task RegisterAgentEvent(Type agent, List<Type> eventTypes);
    Task RegisterAgentEvent(string agentName, string description, List<Type> eventTypes);
}

[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AutogenGAgent : GAgentBase<AutoGenAgentState, AutogenEventBase>, IAutogenGAgent
{
    private IStreamProvider StreamProvider => this.GetStreamProvider(CommonConstants.StreamProvider);
    private readonly IRagProvider _ragProvider;
    private readonly int _maxRaiseEventCount = 50;

    public AutogenGAgent(ILogger<AutogenGAgent> logger,
        IRagProvider ragProvider) : base(logger)
    {
        _ragProvider = ragProvider;
    }

    public async Task RegisterAgentEvent(Type agent, List<Type> eventTypes)
    {
        var grainManager = GrainFactory.GetGrain<IAgentDescriptionManager>(GetAgentDescriptionManagerId());
        await grainManager.AddAgentEventsAsync(agent, eventTypes);
    }

    public async Task RegisterAgentEvent(string agentName, string description, List<Type> eventTypes)
    {
        var grainManager = GrainFactory.GetGrain<IAgentDescriptionManager>(GetAgentDescriptionManagerId());
        await grainManager.AddAgentEventsAsync(agentName, description, eventTypes);
    }

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    [EventHandler]
    public async Task ExecuteAsync(AutoGenCreatedEvent eventData)
    {
        Logger.LogInformation(
            $"[AutogenGAgent] received AutoGenCreatedEvent message,taskId is{eventData.EventId}, {JsonSerializer.Serialize(eventData)}");

        List<AutogenMessage> history = new List<AutogenMessage>();
        // var ragResponse = await _ragProvider.RetrieveAnswerAsync(eventData.Content);
        // if (ragResponse.IsNullOrEmpty() == false)
        // {
        //     history.Add(new AutogenMessage(Role.System.ToString(), ragResponse));
        // }

        history.Add(new AutogenMessage(Role.User.ToString(), eventData.Content));

        await PublishEventToExecutor(eventData.EventId, history);

        RaiseEvent(new Create()
        {
            TaskId = eventData.EventId,
            Messages = history,
            CreateTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
        });

        await base.ConfirmEvents();
    }

    [AllEventHandler]
    public async Task HandleEventAsync(EventWrapperBase eventWrapperBase)
    {
        if (eventWrapperBase is EventWrapper<EventBase> eventWrapper)
        {
            var eventId = eventWrapper.EventId;
            if (State.CheckEventIdExist(eventId) == false)
            {
                return;
            }

            Logger.LogInformation(
                $"[AutogenGAgent] receive reply, eventId:{eventId}, receive message is{eventWrapper.Event}");

            var eventInfo = State.GetEventInfoByEventId(eventId);
            if (eventInfo == null)
            {
                Logger.LogWarning(
                    $"[AutogenGAgent] receive reply but not found eventInfo, eventId:{eventId}, receive message is{eventWrapper.Event}");
                return;
            }

            var agentName = eventInfo.AgentName;
            var eventName = eventInfo.EventName;
            var content = JsonConvert.SerializeObject(eventWrapper.Event);
            
            var reply =
                $"The {eventName} of {agentName} has been processed, the response of {eventName} is: {content}. You must understand the content of the response in JSON format。The input for the next request may depend on the JSON data in the response.";

            var taskInfo = State.GetStateInfoByEventId(eventId);
            if (taskInfo == null)
            {
                Logger.LogWarning(
                    $"[AutogenGAgent] receive reply but not found taskInfo, eventId:{eventId}, taskId{taskInfo.TaskId}, receive message is{eventWrapper.Event}");
                return;
            }

            RaiseEvent(new CallAgentReply()
            {
                EventId = eventId,
                Reply = new AutogenMessage(Role.Assistant.ToString(), reply)
            });

            await base.ConfirmEvents();

            if (State.CheckIsRunning(taskInfo.TaskId))
            {
                return;
            }

            await PublishEventToExecutor(taskInfo.TaskId, taskInfo.ChatHistory);
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(SubscribedEventListEvent subscribedEventListEvent)
    {
        if (subscribedEventListEvent.Value.IsNullOrEmpty())
        {
            return;
        }

        foreach (var (agentType, eventTypeList) in subscribedEventListEvent.Value)
        {
            await RegisterAgentEvent(agentType, eventTypeList);
        }
    }

    [EventHandler]
    public async Task ExecuteAsync(AutoGenExecutorEvent eventData)
    {
        switch (eventData.ExecuteStatus)
        {
            case TaskExecuteStatus.Progressing:
                base.RaiseEvent(new CallerProgressing()
                {
                    TaskId = eventData.TaskId,
                    CurrentCallInfo = eventData.CurrentCallInfo,
                });
                break;
            case TaskExecuteStatus.Break:
                Logger.LogDebug(
                    $"[AutogenGAgent] Task Break,TaskId:{eventData.TaskId}, finish content:{eventData.EndContent}");
                RaiseEvent(new Break()
                {
                    TaskId = eventData.TaskId,
                    BreakReason = eventData.EndContent
                });
                break;
            case TaskExecuteStatus.Finish:
                Logger.LogDebug(
                    $"[AutogenGAgent] Task Finished,TaskId:{eventData.TaskId}, finish content:{eventData.EndContent}");
                RaiseEvent(new Complete()
                {
                    TaskId = eventData.TaskId,
                    Summary = eventData.EndContent
                });
                break;
        }

        await base.ConfirmEvents();
    }

    private async Task PublishEventToExecutor(Guid taskId, List<AutogenMessage> history)
    {
        var grain = GrainFactory.GetGrain<IAutoGenExecutor>(Guid.NewGuid());
        await SubscribeStream(grain);
        _ = grain.ExecuteTaskAsync(new ExecutorTaskInfo()
            { TaskId = taskId, History = history, AgentDescriptionManagerId = GetAgentDescriptionManagerId() });
    }

    private async Task SubscribeStream(IGrainWithGuidKey grain)
    {
        var streamId = StreamId.Create(CommonConstants.StreamNamespace, grain.GetGrainId().ToString());
        var stream = StreamProvider.GetStream<AutoGenInternalEventBase>(streamId);
        await stream.SubscribeAsync(async (message, token) =>
        {
            if (message is AutoGenExecutorEvent @event1)
            {
                await ExecuteAsync(@event1);
            }

            if (message is PassThroughExecutorEvent @event2)
            {
                var taskInfo = State.GetStateInfoByTaskId(@event2.TaskId);
                if (taskInfo == null)
                {
                    return;
                }

                if (taskInfo.RaiseEventCount >= _maxRaiseEventCount)
                {
                    var userInput = taskInfo.ChatHistory.First(f => f.Role == Role.User.ToString());
                    Logger.LogWarning(
                        $"[AutogenGAgent] Raise event Limit, TaskId:{event2.TaskId},input message is :{userInput.Content}");
                    return;
                }

                var eventId = await PublishAsync(@event2.PassThroughData as EventBase);
                Logger.LogInformation(
                    $"[AutogenGAgent] Publish Event, EventId{@event2.TaskId.ToString()}, eventId:{eventId.ToString()}, publish content: {JsonSerializer.Serialize(@event2.PassThroughData)}");

                RaiseEvent(new PublishEvent()
                {
                    TaskId = @event2.TaskId,
                    EventId = eventId,
                    AgentName = @event2.AgentName,
                    EventName = @event2.EventName
                });

                await base.ConfirmEvents();
            }
        });
    }

    private string GetAgentDescriptionManagerId()
    {
        return "AgentDescription" + this.GetPrimaryKey().ToString();
    }
}
