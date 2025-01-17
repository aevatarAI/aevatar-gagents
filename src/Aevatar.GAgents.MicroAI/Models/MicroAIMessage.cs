using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.MicroAI.Model;

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