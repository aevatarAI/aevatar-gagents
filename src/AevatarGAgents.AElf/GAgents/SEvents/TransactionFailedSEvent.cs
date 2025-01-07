using System;
using Orleans;

namespace AevatarGAgents.AElf.Agent.GEvents;
[GenerateSerializer]
public  class TransactionFailedSEvent : CreateTransactionSEvent
{
    [Id(1)] public Guid CreateTransactionGEventId { get; set; }
    [Id(2)] public string Error { get; set; }
}