using System;

namespace AevatarGAgents.Autogen.Common;

[Serializable]
public class AgentResponse<T>
{
    public string AgentName { get; set; }
    public string EventName { get; set; }
    public T Response { get; set; }
}