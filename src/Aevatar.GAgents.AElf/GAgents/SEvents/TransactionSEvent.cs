using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent.GEvents;

[GenerateSerializer]
public class TransactionSEvent : StateLogEventBase<TransactionSEvent>
{
    [Id(0)] public override Guid Id { get; set; } = Guid.NewGuid();
}