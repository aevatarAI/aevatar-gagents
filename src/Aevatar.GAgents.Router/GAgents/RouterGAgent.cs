using System.ComponentModel;
using System.Reflection;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Router.GAgents.Features.Common;
using Aevatar.GAgents.Router.GAgents.SEvents;
using Aevatar.GAgents.Router.GEvents;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.GAgents.Router.GAgents;

public interface IRouterGAgent : IAIGAgent, IGAgent
{
    public Task<RouterGAgentState> GetStateAsync();
    public Task AddAgentDescription(Type agentType,  List<Type> eventList);
}

public class RouterGAgent : AIGAgentBase<RouterGAgentState, RouterGAgentSEvent>, IRouterGAgent
{
    private readonly ILogger<RouterGAgent> _logger;
    
    public RouterGAgent(ILogger<RouterGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "This agent is responsible for generating and managing workflow.");
    }
    
    public async Task<RouterGAgentState> GetStateAsync()
    {
        return State;
    }
    
    private async Task<string?>? InvokeLLMAsync(string prompt)
    {
        var result = await ChatWithHistory(prompt);
        return result?[0].Content;
    }
    
    public new async Task<bool> InitializeAsync(InitializeDto initializeDto)
    {
        await base.InitializeAsync(initializeDto);
        return true;
    }
    
    [EventHandler]
    public async Task RouteNextAsync(RouteNextGEvent eventData)
    {
        _logger.LogInformation("route next {data}", JsonConvert.SerializeObject(eventData));
        if (eventData.CorrelationId == null)
        {
            _logger.LogError("correlationId is empty, event data: {data}", JsonConvert.SerializeObject(eventData));
            return;
        }
        
        var taskId = eventData.CorrelationId.Value;
        
        if (State.TasksInfo.TryGetValue(taskId, out var taskInfo) == false)
        {
            _logger.LogError("task {taskId} info not found", taskId);
            return;
        }
        
        var lastRouterRecord = taskInfo.History.LastOrDefault();
        if (lastRouterRecord == null)
        {
            _logger.LogError("lastRouterRecord is null, taskId {taskId}", taskId);
            return;
        }

        lastRouterRecord.ProcessResult = eventData.ProcessResult;
                
        RaiseEvent(new UpdateHistorySEvent
        {
            TaskId = taskId,
            RouterRecord = lastRouterRecord
        });
        await ConfirmEvents();
        
        await RouteNext(taskId);
    }
    
    [EventHandler]
    public async Task BeginTaskAsync(BeginTaskGEvent eventData)
    {
        _logger.LogInformation("begin task {data}", JsonConvert.SerializeObject(eventData));
        if (eventData.CorrelationId == null)
        {
            _logger.LogError("correlationId is empty");
            return;
        }
        
        var taskId = eventData.CorrelationId.Value;
        if (State.TasksInfo.ContainsKey(taskId))
        {
            _logger.LogError("taskId {taskId} already exists", taskId);
            return;
        }
        
        RaiseEvent(new SetTaskInfoSEvent()
        {
            TaskId = taskId,
            TaskDescription = eventData.TaskDescription
        });
        await ConfirmEvents();
        
        await RouteNext(taskId);
    }
    
    private async Task RouteNext(Guid taskId)
    {
        if (State.TasksInfo.TryGetValue(taskId, out var taskInfo) == false)
        {
            _logger.LogError("taskId {taskId} not found", taskId);
            return;
        }
        
        var taskDescription = taskInfo.TaskDescription;
        var history = taskInfo.History;

        var prompt = GeneratePrompt(taskDescription, history);
        var result = await InvokeLLMAsync(prompt)!;
        if (result == null)
        {
            _logger.LogError("Generate next step failed, taskId: {taskId}, task description: {description}, " +
                             "history: {history}", 
                taskId, taskDescription, JsonConvert.SerializeObject(history));
            return;
        }
        
        var llmOutput = JsonConvert.DeserializeObject<RouterOutputSchema>(result);
        if (llmOutput == null)
        {
            _logger.LogError("incorrect format of result, taskId: {taskId}, task description: {description}, " +
                             "history: {history}, output: {output}", 
                taskId, taskDescription, JsonConvert.SerializeObject(history), result);
            return;
        }
        
        if (llmOutput.Terminated)
        {
            _logger.LogError("task has been terminated, taskId: {taskId}, task description: {description}, " +
                             "history: {history}, reason: {reason}", 
                taskId, taskDescription, JsonConvert.SerializeObject(history), llmOutput.Reason);
            await RemoveTask(taskId);
            return;
        }

        if (llmOutput.Completed)
        {
            _logger.LogInformation("task has been completed, taskId: {taskId}, task description: {description}, " +
                             "history: {history}, reason: {reason}", 
                taskId, taskDescription, JsonConvert.SerializeObject(history), llmOutput.Reason);
            return;
        }
        
        _logger.LogInformation("route next, taskId: {taskId}, task description: {description}, " +
                               "history: {history}, output: {output}",  
            taskId, taskDescription, JsonConvert.SerializeObject(history), result);
        await PublishNextEvent(llmOutput, taskId);
    }
    
    private async Task RemoveTask(Guid taskId)
    {
        RaiseEvent(new RemoveTaskSEvent()
        {
            TaskId = taskId
        });
        await ConfirmEvents();
    }
    
    private string GeneratePrompt(
        string taskDescription, 
        List<RouterRecord> eventHistory)
    {
        // Generate Event History List as JSON formatted string
        string eventHistoryList = string.Join(",\n", eventHistory.Select(JsonConvert.SerializeObject));

        // Generate Agent Descriptions List
        var agentsDescriptionList = string.Join("\n", State.AgentDescriptions.Values.Select(agent =>
        {
            // Agent description
            var agentInfo = $"Agent Name: {agent.AgentName}\n" +
                            $"Description: {agent.AgentDescription}\n" +
                            $"Events:\n";

            // Agent events
            var eventsInfo = string.Join("\n", agent.EventList.Select(eventItem =>
            {
                var parameterDetails = string.Join("; ", eventItem.EventParameters.Select(param =>
                    $"Name: {param.FieldName}, Description: {param.FieldDescription}, Type: {param.FieldType}"));

                return $"- Event: {eventItem.EventName}\n  Description: {eventItem.EventDescription}\n  Parameters: [{parameterDetails}]";
            }));

            return agentInfo + eventsInfo;
        }));

        // Build the prompt
//         var prompt = $@"
// You are a task routing assistant for a multi-agent system. Your goal is to determine the next agent and event needed to achieve the user's task.
//
// ### User Task Description:
// {taskDescription}
//
// ### Processed Event History:
// Here is the list of events that have already been triggered and processed:
// [{eventHistoryList}]
//
// ### Available Agents and Their Events:
// Below is a description of all available agents and the events they can handle, along with their input parameters.
// {agentsDescriptionList}
//
// ### Output Requirements:
// 1. Select ONE event from available agents that logically follows the event history
// 2. Parameters MUST match the exact structure for the selected event type
// 3. If you could select one reasonable event, please output the Json format:
// {{
//     ""agentName"": ""<AgentName>"",
//     ""eventName"": ""<EventName>"",
//     ""parameters"": ""<Parameters JSON String>"",
//     ""reason"": ""<Reason for selecting this event>"",
// }}
// 4. If the user's request is completed, please output the Json format:
//    {{
//         ""completed"": ""true"",
//         ""reason"": ""<Reason you think it is completed>"",
//    }}
// 5. If the user's request is not completed and there is not a reasonable event to follow, please output the Json format:
//    {{
//         ""terminated"": ""true"",
//         ""reason"": ""<Reason for terminate>"",
//    }}
// 6. Your output should be a pure json format, without any other text, which can be deserialize by c# JsonConvert.DeserializeObject directly.
//
// ### Examples
// Valid response example for creating a tweet:
// {{
//   ""AgentName"": ""TwitterGAgent"",
//   ""EventName"": ""PostTweetEvent"",
//   ""Parameters"": ""{{\""TweetContent\"":\""Today's weather is sunny with 25°C. Perfect day!\""}}"",
//   ""Reason"": ""TwitterGAgent could post tweets""
// }}";

        var prompt = PromptTemplate.RouterPrompt
            .Replace("{TASK_DESCRIPTION}", taskDescription)
            .Replace("{EVENT_HISTORY_LIST}", eventHistoryList)
            .Replace("{AGENTS_DESCRIPTION_LIST}", agentsDescriptionList);
        return prompt;
    }

    private async Task PublishNextEvent(RouterOutputSchema routerOutput, Guid taskId)
    {
        if (State.AgentDescriptions.TryGetValue(routerOutput.AgentName, out var agentDescription) == false)
        {
            _logger.LogError("agent {agentName} not found, task: {taskId}, router output: {output}", 
                routerOutput.AgentName, taskId, JsonConvert.SerializeObject(routerOutput));
            return;
        }

        var eventInfo = agentDescription.EventList.FirstOrDefault(f => f.EventName == routerOutput.EventName);
        if (eventInfo == null)
        {
            _logger.LogError("event {eventName} not found, task: {taskId}, router output: {output}", 
                routerOutput.EventName, taskId,  JsonConvert.SerializeObject(routerOutput));
            return;
        }
        
        var eventData = JsonConvert.DeserializeObject(routerOutput.Parameters, eventInfo.EventType) as EventBase;
        if (eventData == null)
        {
            _logger.LogError("event {eventName} deserialize failed, task: {taskId}, router output: {output}", 
                routerOutput.EventName, taskId, JsonConvert.SerializeObject(routerOutput));
            return;
        }
        
        _logger.LogInformation("publish event {eventName}, task: {taskId}, content: {content}", 
            routerOutput.EventName, taskId, JsonConvert.SerializeObject(eventData));
        await PublishAsync(eventData);
        
        RaiseEvent(new AddHistorySEvent
        {
            TaskId = taskId,
            RouterRecord = new RouterRecord
            {
                AgentName = routerOutput.AgentName,
                EventName = routerOutput.EventName,
                Parameters = routerOutput.Parameters
            }
        });
        await ConfirmEvents();
    }
    
    public async Task AddAgentDescription(Type agentType,  List<Type> eventList)
    {
        var agentDescriptionInfo = GetAgentDescriptionAsync(agentType, eventList);
        RaiseEvent(new AddAgentDescriptionSEvent
        {
            AgentName = agentType.Name,
            AgentDescriptionInfo = agentDescriptionInfo
        });
        await ConfirmEvents();
    }
    
    [EventHandler]
    public async Task HandleEventAsync(SubscribedEventListEvent eventData)
    {
        var parent = await this.GetParentAsync();
        if (eventData.PublisherGrainId != parent)
        {
            return;
        }
        
        var agentDescriptionDict = new Dictionary<string, AgentDescriptionInfo>();
        foreach (var item in eventData.Value)
        {
            if (!agentDescriptionDict.ContainsKey(item.Key.Name))
            {
                var agentDescription = GetAgentDescriptionAsync(item.Key, item.Value);
                agentDescriptionDict.Add(item.Key.Name, agentDescription);
            }
        }
        
        RaiseEvent(new SetAgentDescriptionSEvent()
        {
            AgentDescriptions = agentDescriptionDict
        });
        await ConfirmEvents();
    }
    
    private AgentDescriptionInfo GetAgentDescriptionAsync(Type agentType, List<Type> eventTypes)
    {
        var agentDescription = new AgentDescriptionInfo();
        agentDescription.AgentName = agentType.Name;
        var description = agentType.GetCustomAttribute<DescriptionAttribute>();
        if (description == null)
        {
            _logger.LogError("agent:{agentName} description does not exist", agentType.Name);
            agentDescription.AgentDescription = agentType.Name;
        }
        else
        {
            agentDescription.AgentDescription = description.Description;
        }

        foreach (var eventType in eventTypes)
        {
            var eventDescription = GetEventDescription(agentType.Name, eventType);
            if (eventDescription == null)
            {
                continue;
            }

            agentDescription.EventList.Add(eventDescription);
        }

        return agentDescription;
    }
    
    private AgentEventDescription? GetEventDescription(string agentName, Type eventType)
    {
        var result = new AgentEventDescription
        {
            EventName = eventType.Name
        };
        
        var description = eventType.GetCustomAttribute<DescriptionAttribute>();
        if (description == null)
        {
            _logger.LogError("agentName:{agentName} event:{eventName} does not contain DescriptionAttribute", 
                agentName, eventType.Name);
            return null;
        }

        result.EventDescription = description.Description;
        result.EventType = eventType;
        var fields = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var field in fields)
        {
            var eventDescription = GetEventTypeDescription(agentName, eventType.Name, field);
            if (eventDescription == null)
            {
                return null;
            }

            result.EventParameters.Add(eventDescription);
        }

        _logger.LogInformation("agentName:{agentName} event:{eventName} description:{description}", 
            agentName, eventType.Name, result.EventDescription);
        return result;
    }
    
    private AgentEventTypeFieldDescription? GetEventTypeDescription(string agentName, string eventName,
        PropertyInfo fieldType)
    {
        var descriptionAttributes = fieldType.GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (descriptionAttributes.Length == 0)
        {
            _logger.LogError(
                "agentName:{agentName} eventName:{eventName} field:{field} description not exist", 
                agentName, eventName, fieldType.Name);
            return null;
        }

        foreach (var description in descriptionAttributes)
        {
            if (description is not DescriptionAttribute descriptionAttribute)
            {
                continue;
            }

            var fieldDescription = new AgentEventTypeFieldDescription
            {
                FieldName = fieldType.Name,
                FieldDescription = descriptionAttribute.Description,
                FieldType = fieldType.PropertyType.Name
            };

            return fieldDescription;
        }

        return null;
    }
    
    protected override void AIGAgentTransitionState(RouterGAgentState state,
        StateLogEventBase<RouterGAgentSEvent> @event)
    {

        switch (@event)
        {
            case SetAgentDescriptionSEvent setAgentDescriptionsEvent:
                State.AgentDescriptions = setAgentDescriptionsEvent.AgentDescriptions;
                break;
            case SetTaskInfoSEvent beginTaskSEvent:
                State.TasksInfo[beginTaskSEvent.TaskId] = new TaskInfo
                {
                    TaskDescription = beginTaskSEvent.TaskDescription,
                };
                break;
            case AddHistorySEvent addHistorySEvent:
                if (State.TasksInfo.TryGetValue(addHistorySEvent.TaskId, out var taskToBeAdded) == false)
                {
                    break;
                }
                taskToBeAdded.History.Add(addHistorySEvent.RouterRecord);
                State.TasksInfo[addHistorySEvent.TaskId] = taskToBeAdded;
                break;
            case AddAgentDescriptionSEvent addAgentDescriptionSEvent:
                State.AgentDescriptions[addAgentDescriptionSEvent.AgentName] =
                    addAgentDescriptionSEvent.AgentDescriptionInfo;
                break;
            case UpdateHistorySEvent updateHistorySEvent:
                if (State.TasksInfo.TryGetValue(updateHistorySEvent.TaskId, out var taskToBeUpdated) == false)
                {
                    break;
                }
                var cnt = taskToBeUpdated.History.Count;
                if (cnt == 0)
                {
                    break;
                }
                taskToBeUpdated.History[cnt - 1] = updateHistorySEvent.RouterRecord;
                State.TasksInfo[updateHistorySEvent.TaskId] = taskToBeUpdated;
                break;
            case RemoveTaskSEvent removeTaskSEvent:
                State.TasksInfo.Remove(removeTaskSEvent.TaskId);
                break;
        }
    }
    
}