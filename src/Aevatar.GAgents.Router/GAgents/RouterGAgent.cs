using System.ComponentModel;
using System.Reflection;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic;
using Aevatar.GAgents.Basic.Dtos;
using Aevatar.GAgents.Router.GAgents.Features.Dto;
using Aevatar.GAgents.Router.GAgents.SEvents;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Router.GAgents;

public interface IRouterGAgent : IAIGAgent, IGAgent
{
}

public class RouterGAgent : AIGAgentBase<RouterGAgentState, RouterGAgentSEvent>, IRouterGAgent
{
    private readonly ILogger<RouterGAgent> _logger;
    
    public RouterGAgent(ILogger<RouterGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "This agent is responsible for generating and managing workflow.");
    }
    
    public new async Task<bool> InitializeAsync(InitializeDto initializeDto)
    {
        //save state
        await base.InitializeAsync(initializeDto);
        return true;
    }
    
    [EventHandler]
    public async Task HandleEventAsync(SubscribedEventListEvent eventData)
    {
        var agentDescriptionDict = new Dictionary<string, AgentDescriptionInfo>();
        foreach (var item in eventData.Value)
        {
            if (!agentDescriptionDict.ContainsKey(item.Key.Name))
            {
                var agentDescription = await GetAgentDescriptionAsync(item.Key, item.Value);

                if (agentDescription != null)
                {
                    agentDescriptionDict.Add(item.Key.Name, agentDescription);
                }
            }
        }
        
        RaiseEvent(new SetAgentDescriptionsEvent()
        {
            Id = Guid.NewGuid(),
            Agents = eventData.Value,
            AgentDescriptions = agentDescriptionDict
        });
        await ConfirmEvents();
    }
    
    private async Task<AgentDescriptionInfo?> GetAgentDescriptionAsync(Type agentType, List<Type> eventTypes)
    {
        var agentDescription = new AgentDescriptionInfo();
        agentDescription.AgentName = agentType.Name;
        var description = agentType.GetCustomAttribute<DescriptionAttribute>();
        if (description == null)
        {
            _logger.LogError("agent:{agentName} description does not exist", agentType.Name);
            return null;
        }

        agentDescription.AgentDescription = description.Description;

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
        var result = new AgentEventDescription();
        result.EventName = eventType.Name;
        var description = eventType.GetCustomAttribute<DescriptionAttribute>();
        if (description == null)
        {
            _logger.LogError("agentName:{agentName} event:{eventName} does not contain DescriptionAttribute", 
                agentName, eventType.Name);
            return null;
        }

        result.EventDescription = description.Description;
        result.EventType = eventType;
        var fields = eventType.GetProperties();
        foreach (var field in fields)
        {
            var eventDescription = GetEventTypeDescription(agentName, eventType.Name, field);
            if (eventDescription == null)
            {
                return null;
            }

            result.EventParameters.Add(eventDescription);
        }

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
            case SetAgentDescriptionsEvent setAgentDescriptionsEvent:
                State.AgentsInGroup = setAgentDescriptionsEvent.Agents;
                break;
        }
    }
    
}