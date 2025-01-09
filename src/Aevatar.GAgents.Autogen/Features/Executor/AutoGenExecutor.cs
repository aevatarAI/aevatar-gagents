
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Autogen.Common;
using Aevatar.GAgents.Autogen.DescriptionManager;
using Aevatar.GAgents.Autogen.Events.InternalEvents;
using Aevatar.GAgents.Autogen.Exceptions;
using AutoGen.Core;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using IStreamProvider = Orleans.Streams.IStreamProvider;

namespace Aevatar.GAgents.Autogen.Executor;

public class AutoGenExecutor : Grain, IAutoGenExecutor
{
    private IStreamProvider StreamProvider => this.GetStreamProvider(CommonConstants.StreamProvider);
    private readonly IChatAgentProvider _chatAgentProvider;
    private readonly ILogger<AutoGenExecutor> _logger;
    private const string AgentName = "admin";
    private const string FinishFlag = "complete";
    private const string BreakFlag = "break";
    private Guid _taskId;
    private IAgentDescriptionManager _descriptionManager;

    public AutoGenExecutor(ILogger<AutoGenExecutor> logger, IChatAgentProvider chatAgentProvider)
    {
        _logger = logger;
        _chatAgentProvider = chatAgentProvider;
    }

    public async Task ExecuteTaskAsync(ExecutorTaskInfo taskInfo)
    {
        try
        {
            await CallAutogen(taskInfo);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AutogenExecutor] ExecuteTaskAsync error, TaskInfo:{@TaskInfo}", taskInfo);
            await PublishInternalEvent(new AutoGenExecutorEvent()
            {
                TaskId = _taskId,
                ExecuteStatus = TaskExecuteStatus.Break,
                EndContent = "Internal error",
            });
        }
    }

    public async Task CallAutogen(ExecutorTaskInfo taskInfo)
    {
        _logger.LogDebug(
            $"[AutoGenExecutor] receive task:{taskInfo.TaskId.ToString()}");
        _taskId = taskInfo.TaskId;
        _descriptionManager = GrainFactory.GetGrain<IAgentDescriptionManager>(taskInfo.AgentDescriptionManagerId);
        
        var history = ConvertMessage(taskInfo.History);
        var responsibility = await GetAgentResponsibility();
        _chatAgentProvider.SetAgent(AgentName, responsibility, GetMiddleware());
        var response = await _chatAgentProvider.SendAsync(AgentName, "What should be done next?", history);
        if (response == null)
        {
            _logger.LogDebug(
                $"[AutoGenExecutor] autoGen response is null, History:{JsonSerializer.Serialize(history)}");
            return;
        }

        var responseStr = response.GetContent();
        if (responseStr.IsNullOrEmpty())
        {
            _logger.LogDebug(
                $"[AutoGenExecutor] autoGen responseStr is null, History:{JsonSerializer.Serialize(history)}");
            return;
        }

        if (responseStr.Contains(FinishFlag))
        {
            var completeEvent = JsonSerializer.Deserialize<EventComplete>(responseStr);
            if (completeEvent != null)
            {
                // var publishGrain = _clusterClient.GetGrain<IPublishingGAgent>(_publishGrainId);
                await PublishInternalEvent(new AutoGenExecutorEvent()
                {
                    TaskId = _taskId,
                    ExecuteStatus = TaskExecuteStatus.Finish,
                    EndContent = completeEvent.Complete,
                });
                return;
            }

            _logger.LogDebug(
                $"[AutoGenExecutor] response is finished,but Deserialize error, History:{JsonSerializer.Serialize(history)} response:{responseStr}");
        }
        else if (responseStr.Contains(BreakFlag))
        {
            var breakInfo = JsonSerializer.Deserialize<EventBreak>(responseStr);
            if (breakInfo != null)
            {
                // var publishGrain = _clusterClient.GetGrain<IPublishingGAgent>(_publishGrainId);
                await PublishInternalEvent(new AutoGenExecutorEvent()
                {
                    TaskId = _taskId,
                    ExecuteStatus = TaskExecuteStatus.Break,
                    EndContent = breakInfo.Break,
                });
                return;
            }

            _logger.LogDebug(
                $"[AutoGenExecutor] response is break,but Deserialize error, History:{JsonSerializer.Serialize(history)} response:{responseStr}");
        }

        var responseEvents = responseStr.SplitToLines();
        foreach (var responseEvent in responseEvents)
        {
            var handleEventSchema = JsonSerializer.Deserialize<HandleEventAsyncSchema>(responseEvent);
            if (handleEventSchema != null)
            {
                // var publishGrain = _clusterClient.GetGrain<IPublishingGAgent>(_publishGrainId);
                await PublishInternalEvent(new AutoGenExecutorEvent()
                {
                    TaskId = _taskId,
                    ExecuteStatus = TaskExecuteStatus.Progressing,
                    CurrentCallInfo = responseEvent
                });
            }
        }
    }

    private List<IMessage> ConvertMessage(List<AutogenMessage> listAutoGenMessage)
    {
        var result = new List<IMessage>();
        foreach (var item in listAutoGenMessage)
        {
            result.Add(new TextMessage(GetRole(item.Role), item.Content));
        }

        return result;
    }

    private Role GetRole(string roleName)
    {
        switch (roleName)
        {
            case "user":
                return Role.User;
            case "assistant":
                return Role.Assistant;
            case "system":
                return Role.System;
            case "function":
                return Role.Function;
            default:
                return Role.User;
        }
    }

    private FunctionCallMiddleware GetMiddleware()
    {
        var groupChatContract = new List<FunctionContract>()
        {
            HandleEventAsyncContract
        };

        var groupChatFunctionMap = new Dictionary<string, Func<string, Task<string>>>()
        {
            { nameof(HandleEventAsync), HandleEventAsyncWrapper }
        };

        return new FunctionCallMiddleware(groupChatContract, groupChatFunctionMap);
    }

    private async Task<string> GetAgentResponsibility()
    {
        var description = await _descriptionManager.GetAutoGenEventDescriptionAsync();

        return $$"""
                 You are a manager who solves user problems by organizing agents,
                 - The following is a JSON-formatted description of all agents, including the events each proxy can handle and the parameters for each event:
                 {{description}}
                 JSON explanation:
                 "AgentName":Agent's name
                 "AgentDescription":Agent's responsibilities
                    "EventName":Event's name
                    "EventDescription":Things the event can handle
                        "EventParameters":
                            "FieldName":Parameter's name
                            "FieldDescription":Parameter's description
                            "FieldType":Parameter's type

                 - You can understand what the event can do through the event description, and know the type, name, and description of each parameter through the event parameters. 
                 If the event description can handle the user's request, you need to assemble the request into the following JSON format:
                 [{
                     AgentName:"",
                     EventName:"",
                     Parameters:""
                 }]
                 JSON explanation:
                 "AgentName" is the name of the proxy to be called. 
                 "EventName" is the name of the event to be invoked on the proxy, 
                 "Parameters" are the parameters for the event. The parameter types and names must match the defined ones.
                 - If there are multiple events returned, please assemble them into a JSON array.

                 The workflow is as follows:
                 - You take the problem from user.
                 - If an agent can handle the message,, please do not modify the data.
                    For Example:
                        If the voting agent can handle tasks related to voting. If the input is "Do you prefer swimming or working out?", the data should be passed to the voting agent in its entirety.the voting agent should not receive "I prefer swimming.".
                        
                 - Split the task into different events, and use the output of the previous event combined with the user's request to determine the execution of the next event. 
                 - Based on the event's response, reorganize the next request in line with the user's intent.
                 - If the event has already been clearly processed, then this event should not be called again in the next step and if there is no next event, it will terminate.
                 For Example: 
                     If event response:"The VoteEvent of VoteAgent has been processed, the response of VoteEvent is: {}. The input for the next request may depend on the JSON data in the response."
                     "{}" means that the VoteEvent has already been completed and no return value, so the next step cannot call the VoteEvent again. If there are no other events to process afterward, then it will terminate.
                    
                 - If the above events cannot meet the user's task execution needs, you can generate results based on the events to drive the continuation of the process.
                 - The response for each event will be added to the conversation in JSON format.
                   You need to analyze the response information to decide whether to proceed to the next round. 
                   The response information will be used during the final summary. The JSON format is as follows:
                   {
                    "AgentName":"", 
                    "EventName":"",
                    "Response":{}
                   }
                   JSON explanation:
                   AgentName is the caller of Agent.
                   EventName is the name of the agent's event.
                   Response is the response from the event corresponding to agent's EventName,It could be in JSON format,You need to understand the meaning of each field and potentially use the values as parameters for the next event.
                 - If the user's request is completed, please output the Json format:
                    {
                        "complete":"{reply summary}"
                    }
                    
                 - If the user's request cannot continue, please output the Json format:
                    {
                        "break": "{question}"
                    }
                 """;
    }

    private async Task PublishInternalEvent(AutoGenInternalEventBase publishData)
    {
        var streamId = StreamId.Create(CommonConstants.StreamNamespace, this.GetGrainId().ToString());
        var stream = StreamProvider.GetStream<AutoGenInternalEventBase>(streamId);
        await stream.OnNextAsync(publishData);
    }

    #region Event hook

    /// <summary>
    /// handle middleware event
    /// </summary>
    /// <param name="agentName">agent name</param>
    /// <param name="eventName">event name</param>
    /// <param name="parameters">event parameter json string</param>
    /// <returns></returns>
    [Function]
    public async Task<string> HandleEventAsync(string agentName, string eventName, string parameters)
    {
        var descriptionDic = await _descriptionManager.GetAgentDescription();
        if (descriptionDic.TryGetValue(agentName, out var eventDescription) == false)
        {
            throw new AutogenException($"Event name:{agentName} not exist");
        }

        var eventInfo = eventDescription.EventList.FirstOrDefault(f => f.EventName == eventName);
        if (eventInfo == null)
        {
            throw new AutogenException(
                $"agentName:{agentName} Event name:{eventName} Deserialize object is null, the parameters is{parameters}");
        }

        var eventData = JsonSerializer.Deserialize(parameters, eventInfo.EventType) as EventBase;
        if (eventData == null)
        {
            throw new AutogenException(
                $"Event name:{agentName} Deserialize object is null, the parameters is{parameters}");
        }

        var callEventOrignalData = JsonSerializer.Serialize(new HandleEventAsyncSchema()
            { AgentName = agentName, EventName = eventName, Parameters = parameters });

        // var publishGrain = _clusterClient.GetGrain<IPublishingGAgent>(_publishGrainId);
        await PublishInternalEvent(new PassThroughExecutorEvent()
            { AgentName = agentName, EventName = eventName, PassThroughData = eventData, TaskId = _taskId });

        return callEventOrignalData;
    }

    public FunctionContract HandleEventAsyncContract
    {
        get => new FunctionContract
        {
            ClassName = nameof(AutogenGAgent),
            Name = nameof(HandleEventAsync),
            Description = @"Handle agent's event",
            ReturnType = typeof(Task<string>),
            ReturnDescription = "event handle result",
            Parameters = new global::AutoGen.Core.FunctionParameterContract[]
            {
                new FunctionParameterContract
                {
                    Name = @"agentName",
                    Description = @"agent name",
                    ParameterType = typeof(string),
                    IsRequired = true,
                },
                new FunctionParameterContract
                {
                    Name = @"eventName",
                    Description = @"event name",
                    ParameterType = typeof(string),
                    IsRequired = true,
                },
                new FunctionParameterContract()
                {
                    Name = @"parameters",
                    Description = @"event parameters",
                    ParameterType = typeof(string),
                    IsRequired = true,
                }
            },
        };
    }

    public async Task<string> HandleEventAsyncWrapper(string arguments)
    {
        var schema = JsonSerializer.Deserialize<HandleEventAsyncSchema>(
            arguments,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

        string result;
        try
        {
            _logger.LogInformation($"[AutogenExecutor] received autogen dispatch, parameter:{arguments}");
            result = await HandleEventAsync(schema.AgentName, schema.EventName, schema.Parameters);
        }
        catch (AutogenException e)
        {
            result = JsonSerializer.Serialize(new EventBreak() { Break = e.Message });
        }

        return result;
    }

    public class EventBreak
    {
        [JsonPropertyName(@"break")] public string Break { get; set; }
    }

    public class EventComplete
    {
        [JsonPropertyName(@"complete")] public string Complete { get; set; }
    }

    #endregion
}