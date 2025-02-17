using System.Text.Json.Serialization;

namespace Aevatar.GAgents.Router.GAgents.Features.Dto;

[GenerateSerializer]
public class RouterOutputSchema
{
    [Id(0)] [JsonPropertyName(@"agentName")] public string AgentName { get; set; }
    [Id(1)] [JsonPropertyName(@"eventName")] public string EventName { get; set; }
    [Id(2)] [JsonPropertyName(@"parameters")] public string Parameters { get; set; }
}