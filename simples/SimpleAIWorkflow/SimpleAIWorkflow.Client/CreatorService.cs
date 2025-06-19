using System.Reflection;
using Aevatar.Core.Abstractions;
using Aevatar.Station.Feature.CreatorGAgent;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SimpleAIWorkflow.Client;

public class CreatorService
{
    private readonly IGAgentFactory _gAgentFactory;

    public CreatorService(IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
    }

    public async Task CreateAgentAsync(CreateAgentInputDto dto)
    {
        var guid = dto.AgentId ?? Guid.NewGuid();
        var agentData = new AgentData
        {
            UserId = new Guid(), // User Current id
            AgentType = dto.AgentType,
            Name = dto.Name
        };

        var initializationParam =
            dto.Properties.IsNullOrEmpty() ? string.Empty : JsonConvert.SerializeObject(dto.Properties);
        var (businessAgent, properties) = await InitializeBusinessAgent(guid, dto.AgentType, initializationParam);
        var creatorAgent = await _gAgentFactory.GetGAgentAsync<ICreatorGAgent>(guid);
        agentData.BusinessAgentGrainId = businessAgent.GetGrainId();
        agentData.Properties = JsonConvert.SerializeObject(properties, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        await creatorAgent.CreateAgentAsync(agentData);
    }

    private async Task<Tuple<IGAgent, ConfigurationBase>> InitializeBusinessAgent(Guid primaryKey, string agentType,
        string agentProperties)
    {
        var grainId = GrainId.Create(agentType, primaryKey.ToString("N"));
        var businessAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        var initializationData = await GetAgentConfigurationAsync(businessAgent);
        var config = SetupConfigurationData(initializationData, agentProperties);
        await businessAgent.ConfigAsync(config);
        return new Tuple<IGAgent, ConfigurationBase>(businessAgent, config);
    }

    private async Task<Configuration?> GetAgentConfigurationAsync(IGAgent agent)
    {
        var configurationType = await agent.GetConfigurationTypeAsync();
        if (configurationType == null || configurationType.IsAbstract)
        {
            return null;
        }

        PropertyInfo[] properties =
            configurationType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                            BindingFlags.DeclaredOnly);

        var configuration = new Configuration
        {
            DtoType = configurationType
        };

        var propertyDtos = new List<PropertyData>();
        foreach (PropertyInfo property in properties)
        {
            var propertyDto = new PropertyData()
            {
                Name = property.Name,
                Type = property.PropertyType
            };
            propertyDtos.Add(propertyDto);
        }

        configuration.Properties = propertyDtos;
        return configuration;
    }


    private ConfigurationBase SetupConfigurationData(Configuration configuration,
        string propertiesString)
    {
        var actualDto = Activator.CreateInstance(configuration.DtoType);
        var config = (ConfigurationBase)actualDto!;
        config = JsonConvert.DeserializeObject(propertiesString, configuration.DtoType) as ConfigurationBase;
        return config;
    }
}

public class CreateAgentInputDto
{
    public Guid? AgentId { get; set; }
    public string AgentType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

public class Configuration
{
    public Type DtoType { get; set; }
    public List<PropertyData> Properties { get; set; }
}

public class PropertyData
{
    public string Name { get; set; }
    public Type Type { get; set; }
}

// public class AgentDto
// {
//     public Guid Id { get; set; }
//     public string AgentType { get; set; }
//     public string Name { get; set; }
//     public Dictionary<string, object>? Properties { get; set; }
//     public GrainId GrainId { get; set; }
//     public Guid AgentGuid { get; set; }
//     public string PropertyJsonSchema { get; set; }
//     public string BusinessAgentGrainId { get; set; }
// }