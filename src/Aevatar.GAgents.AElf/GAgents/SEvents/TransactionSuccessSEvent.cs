using System;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent.GEvents;

public  class TransactionSuccessSEvent : TransactionSEvent
{
    [Id(1)] public Guid CreateTransactionGEventId { get; set; }
}