using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AISmart.GAgent.Telegram.Agent.GEvents;

[GenerateSerializer]
public class MessageSEvent :GEventBase
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
}