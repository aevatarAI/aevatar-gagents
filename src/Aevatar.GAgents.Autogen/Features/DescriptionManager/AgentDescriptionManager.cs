using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aevatar.GAgents.Autogen.Exceptions;
using AutoGen.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;

namespace Aevatar.GAgents.Autogen.DescriptionManager;

public class AgentDescriptionManager : Grain<AgentDescriptionManagerState>, IAgentDescriptionManager
{
    private readonly ILogger<AgentDescriptionManager> _logger;

    public AgentDescriptionManager(ILogger<AgentDescriptionManager> logger)
    {
        _logger = logger;
    }

    public async Task AddAgentEventsAsync(Type agentType, List<Type> eventTypes)
    {
        if (State.AgentDescription.TryGetValue(agentType.Name, out var agentDescription) == false)
        {
            agentDescription = new AgentDescriptionInfo();
            agentDescription.AgentName = agentType.Name;
            var description = agentType.GetCustomAttribute<DescriptionAttribute>();
            if (description == null)
            {
                _logger.LogError($"[AgentDescriptionManager] agent:{agentType.Name}  description does not exist");
                return;
            }

            agentDescription.AgentDescription = description.Description;

            State.AgentDescription.Add(agentType.Name, agentDescription);
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

        State.AutoGenAgentEventDescription = AssembleAllAgentDescription();

        await WriteStateAsync();
    }

    public async Task AddAgentEventsAsync(string agentName, string agentDescriptionStr, List<Type> eventTypes)
    {
        if (State.AgentDescription.TryGetValue(agentName, out var agentDescription) == false)
        {
            agentDescription = new AgentDescriptionInfo();
            agentDescription.AgentName = agentName;
            agentDescription.AgentDescription = agentDescriptionStr;

            State.AgentDescription.Add(agentName, agentDescription);
        }

        foreach (var eventType in eventTypes)
        {
            var eventDescription = GetEventDescription(agentName, eventType);
            if (eventDescription == null)
            {
                continue;
            }

            agentDescription.EventList.Add(eventDescription);
        }

        State.AutoGenAgentEventDescription = AssembleAllAgentDescription();

        await WriteStateAsync();
    }

    public async Task<ReadOnlyDictionary<string, AgentDescriptionInfo>> GetAgentDescription()
    {
        return new ReadOnlyDictionary<string, AgentDescriptionInfo>(State.AgentDescription);
    }

    public Task<string> GetAutoGenEventDescriptionAsync()
    {
        return Task.FromResult(State.AutoGenAgentEventDescription);
    }

    private AgentEventDescription? GetEventDescription(string agentName, Type eventType)
    {
        var result = new AgentEventDescription();
        result.EventName = eventType.Name;
        var description = eventType.GetCustomAttribute<DescriptionAttribute>();
        if (description == null)
        {
            _logger.LogError($"agentName:{agentName} event:{eventType.Name} does not contain DescriptionAttribute");
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
                $"agentName:{agentName} eventName:{eventName} field:{fieldType.Name} description not exist");
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

    private Dictionary<string, AgentDescriptionInfo> GetAllAgentDescription()
    {
        var result = new Dictionary<string, AgentDescriptionInfo>();

        string currentDirectory = Directory.GetCurrentDirectory();
        string[] dllFiles = Directory.GetFiles(currentDirectory, "*.dll");
        foreach (var dllFilePath in dllFiles)
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(dllFilePath);
                if (assembly == null)
                {
                    continue;
                }

                var dllAgentDescription = GenerateAgentDescription(assembly);
                foreach (var description in dllAgentDescription)
                {
                    if (result.ContainsKey(description.Key))
                    {
                        throw new AutogenException($"Agent name:{description.Key} has exist");
                    }

                    result.Add(description.Key, description.Value);
                }
            }
            catch (Exception e)
            {
                continue;
            }
        }

        return result;
    }

    private Dictionary<string, AgentDescriptionInfo> GenerateAgentDescription(Assembly assembly)
    {
        var result = new Dictionary<string, AgentDescriptionInfo>();
        try
        {
            var agentList = assembly.GetTypes();

            foreach (var classType in agentList)
            {
                if (!classType.IsClass || classType.GetInterface(nameof(IAgent)) == null)
                {
                    continue;
                }

                var agentDescription = new AgentDescriptionInfo();
                var description = classType.GetCustomAttribute<DescriptionAttribute>();
                if (description == null)
                {
                    continue;
                }

                agentDescription.AgentName = classType.Name;
                agentDescription.AgentDescription = description.Description;
                //
                // var agentGenericList = classType.GetGenericArguments();
                // if (agentGenericList.Length != 2)
                // {
                //     throw new Exception($"ClassName:{classType.Name} generic error");
                // }
                //
                // var eventType = agentGenericList[1];
                //
                // // get all field name
                // FieldInfo[] fields = eventType.GetFields(BindingFlags.Public);
                // var fieldList = new List<AgentEventTypeFieldDescription>();
                // foreach (var fieldType in fields)
                // {
                //     var descriptionAttribute = fieldType.GetCustomAttribute<DescriptionAttribute>();
                //     if (descriptionAttribute == null)
                //     {
                //         throw new Exception($"ClassName:{eventType.Name} field:{fieldType.Name} description not exsit");
                //     }
                //
                //     var fieldDescription = new AgentEventTypeFieldDescription();
                //     fieldDescription.FieldName = fieldType.Name;
                //     fieldDescription.FieldDescription = descriptionAttribute.Description;
                //     fieldDescription.FieldType = fieldType.FieldType;
                //     fieldList.Add(fieldDescription);
                // }
                //
                // agentDescription.EventParameters = fieldList;

                result.Add(agentDescription.AgentName, agentDescription);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ex");
        }

        return result;
    }

    private string AssembleAllAgentDescription()
    {
        var availableAgent = State.AgentDescription.Values.Where(s => s.EventList.Count > 0).ToList();
        if (availableAgent.Count > 0)
        {
            var agentsDescription = JsonConvert.SerializeObject(availableAgent);
            return agentsDescription;
        }

        return "{}";
    }

    private string AssembleEventDescription(AgentEventDescription eventDescription)
    {
        var result = new StringBuilder();

        StringBuilder agentDescription = new StringBuilder();
        agentDescription.AppendLine("{");
        agentDescription.AppendLine($"  EventName:\"{eventDescription.EventName}\",");
        agentDescription.AppendLine($"  EventDescription:\"{eventDescription.EventDescription}\",");
        agentDescription.AppendLine($"  EventParameters:[");
        agentDescription.AppendLine($"{AssembleAgentEventParameterDescription(eventDescription.EventParameters)}");
        agentDescription.AppendLine($"  ]");
        agentDescription.AppendLine("}");
        result.Append(agentDescription.ToString());

        return result.ToString();
    }

    private string AssembleAgentEventParameterDescription(List<AgentEventTypeFieldDescription> fieldDescriptionList)
    {
        var result = new StringBuilder();
        var firstFlag = fieldDescriptionList.Count > 1;
        foreach (var item in fieldDescriptionList)
        {
            var fieldDescription = new StringBuilder();
            fieldDescription.AppendLine("   {");
            fieldDescription.AppendLine($"      ParameterType:\"{nameof(item.FieldType)}\",");
            fieldDescription.AppendLine($"      ParameterName:\"{item.FieldName}\",");
            fieldDescription.AppendLine($"      ParameterDescription:\"{item.FieldDescription}\",");
            fieldDescription.AppendLine($"      ParameterIsRequired:true");
            if (firstFlag)
            {
                fieldDescription.AppendLine("   },");
                firstFlag = false;
            }
            else
            {
                fieldDescription.AppendLine("   }");
            }

            result.AppendLine(fieldDescription.ToString());
        }

        return result.ToString();
    }
}