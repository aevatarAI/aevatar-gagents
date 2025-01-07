using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.MicroAI.Agent.GEvents;

[GenerateSerializer]
public class AIMessageGEvent :GEventBase
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
}

[GenerateSerializer]
public class MicroAIMessage
{
    public MicroAIMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }

    [Id(0)]public string Role { get; set; }
    [Id(1)] public string Content { get; set; }
}