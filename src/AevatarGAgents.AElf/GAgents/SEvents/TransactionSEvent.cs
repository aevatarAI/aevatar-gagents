using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.AElf.Agent.GEvents;

[GenerateSerializer]
public class TransactionSEvent : GEventBase
{
    [Id(0)] public override  Guid Id { get; set; } = Guid.NewGuid();
}