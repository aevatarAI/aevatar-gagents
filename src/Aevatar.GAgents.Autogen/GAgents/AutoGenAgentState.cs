using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Autogen.Common;
using Aevatar.GAgents.Autogen.EventSourcingEvent;
using Castle.Components.DictionaryAdapter;
using Orleans;

namespace Aevatar.GAgents.Autogen.State;

[GenerateSerializer]
public class AutoGenAgentState : StateBase
{
    [Id(0)] public Dictionary<Guid, AutoGenAgentStateInfo> AutoGenStateDic =
        new Dictionary<Guid, AutoGenAgentStateInfo>();

    [Id(1)] public Dictionary<Guid, RequestInfo> EventToTaskDic = new Dictionary<Guid, RequestInfo>();
    [Id(2)] public Dictionary<Guid, List<Guid>> TaskToEventDic = new Dictionary<Guid, List<Guid>>();

    public void Apply(Break @event)
    {
        var state = GetStateInfo(@event.TaskId);
        if (state == null)
        {
            return;
        }

        state.SessionStateEnum = SessionStateEnum.Break;
        state.BreakReason = @event.BreakReason;

        CompleteTask(@event.TaskId);
    }

    public bool CheckEventIdExist(Guid eventId)
    {
        return EventToTaskDic.ContainsKey(eventId);
    }

    public AutoGenAgentStateInfo? GetStateInfoByEventId(Guid eventId)
    {
        if (EventToTaskDic.TryGetValue(eventId, out var requestInfo))
        {
            return AutoGenStateDic[requestInfo.TaskId];
        }

        return null;
    }

    public AutoGenAgentStateInfo? GetStateInfoByTaskId(Guid taskId)
    {
        if (AutoGenStateDic.TryGetValue(taskId, out var stateInfo))
        {
            return stateInfo;
        }

        return null;
    }

    public bool CheckIsRunning(Guid taskId)
    {
        if (TaskToEventDic.TryGetValue(taskId, out var taskInfo))
        {
            return taskInfo.Count > 0;
        }

        return false;
    }
    
    public RequestInfo? GetEventInfoByEventId(Guid eventId)
    {
        return EventToTaskDic.TryGetValue(eventId, out var requestInfo) ? requestInfo : null;
    }
    
    public void Apply(CallAgentReply @event)
    {
        if (!EventToTaskDic.TryGetValue(@event.EventId, out var taskList))
        {
            return;
        }

        var state = GetStateInfo(taskList.TaskId);
        if (state == null)
        {
            return;
        }

        CompleteEvent(@event.EventId);
        state.ChatHistory.Add(@event.Reply);
    }

    public void Apply(CallerProgressing @event)
    {
        var state = GetStateInfo(@event.TaskId);
        if (state == null)
        {
            return;
        }

        // StartEvent(@event.TaskId, @event.EventId);
        // state.CurrentCallInfo = @event.CurrentCallInfo;
    }

    public void Apply(Complete @event)
    {
        var state = GetStateInfo(@event.TaskId);
        if (state == null)
        {
            return;
        }

        state.SessionStateEnum = SessionStateEnum.Completed;
        state.Summary = @event.Summary;
        CompleteTask(@event.TaskId);
        AutoGenStateDic.Remove(@event.TaskId);
    }

    public void Apply(Create @event)
    {
        var state = new AutoGenAgentStateInfo();
        state.TaskId = @event.TaskId;
        state.ChatHistory = @event.Messages;
        state.SessionStartTime = @event.CreateTime;

        AutoGenStateDic.Add(@event.TaskId, state);
    }

    public void Apply(PublishEvent @event)
    {
        var state = GetStateInfo(@event.TaskId);
        if (state == null)
        {
            return;
        }

        state.RaiseEventCount += 1;
        StartEvent(@event.TaskId, @event.AgentName, @event.EventName, @event.EventId);
    }

    private AutoGenAgentStateInfo? GetStateInfo(Guid taskId)
    {
        AutoGenStateDic.TryGetValue(taskId, out var result);
        return result;
    }

    private void CompleteEvent(Guid eventId)
    {
        if (EventToTaskDic.TryGetValue(eventId, out var requestInfo) == true)
        {
            if (TaskToEventDic.TryGetValue(requestInfo.TaskId, out var requestList))
            {
                requestList.Remove(eventId);

                if (requestList.Count == 0)
                {
                    TaskToEventDic.Remove(requestInfo.TaskId);
                }
            }

            EventToTaskDic.Remove(eventId);
        }
    }

    private void CompleteTask(Guid taskId)
    {
        if (TaskToEventDic.TryGetValue(taskId, out var eventList))
        {
            foreach (var eventId in eventList)
            {
                EventToTaskDic.Remove(eventId);
            }

            TaskToEventDic.Remove(taskId);
        }

        AutoGenStateDic.Remove(taskId);
    }

    private void StartEvent(Guid taskId, string agentName, string eventName, Guid eventId)
    {
        if (EventToTaskDic.ContainsKey(eventId))
        {
            return;
        }

        EventToTaskDic[eventId] = new RequestInfo()
        {
            TaskId = taskId, StartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), AgentName = agentName,
            EventName = eventName
        };

        if (TaskToEventDic.TryGetValue(taskId, out var eventList))
        {
            eventList.Add(eventId);
        }
        else
        {
            eventList = new EditableList<Guid>() { eventId };
            TaskToEventDic.Add(taskId, eventList);
        }
    }
}

[GenerateSerializer]
public class AutoGenAgentStateInfo
{
    [Id(0)] public Guid TaskId { get; set; }
    [Id(1)] public List<AutogenMessage> ChatHistory { get; set; }
    [Id(2)] public long SessionStartTime { get; set; }
    [Id(3)] public SessionStateEnum SessionStateEnum { get; set; } = SessionStateEnum.Processing;
    [Id(4)] public string CurrentCallInfo { get; set; }
    [Id(5)] public string Summary { get; set; }
    [Id(6)] public string BreakReason { get; set; }
    [Id(7)] public int RaiseEventCount { get; set; }
}

[GenerateSerializer]
public enum SessionStateEnum
{
    Processing,
    Break,
    Completed
}

[GenerateSerializer]
public class RequestInfo
{
    [Id(0)] public Guid TaskId { get; set; }
    [Id(1)] public long StartTime { get; set; }
    [Id(2)] public string AgentName { get; set; }
    [Id(3)] public string EventName { get; set; }
}