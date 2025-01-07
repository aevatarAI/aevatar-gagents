using System.Text.Json.Serialization;

namespace AevatarGAgents.Autogen.Executor;

public class HandleEventAsyncSchema
{
    [JsonPropertyName(@"agentName")] public string AgentName { get; set; }
    [JsonPropertyName(@"eventName")] public string EventName { get; set; }
    [JsonPropertyName(@"parameters")] public string Parameters { get; set; }
}
