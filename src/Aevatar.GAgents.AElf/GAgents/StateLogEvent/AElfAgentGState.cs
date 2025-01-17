using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AElf.Agent.GEvents;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent;

[GenerateSerializer]
public class AElfAgentGState : StateBase
{
    [Id(0)]  public  Guid Id { get; set; }
    
    [Id(1)] public Dictionary<Guid, CreateTransactionStateLogEvent> PendingTransactions { get; set; } = new Dictionary<Guid, CreateTransactionStateLogEvent>();
    
    public void Apply(CreateTransactionStateLogEvent createTransactionStateLogEvent)
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }
        PendingTransactions[createTransactionStateLogEvent.Id] = createTransactionStateLogEvent;
    }
    
    public void Apply(SendTransactionStateLogEvent sendTransactionStateLogEvent)
    {
        PendingTransactions[sendTransactionStateLogEvent.CreateTransactionGEventId].TransactionId =
            sendTransactionStateLogEvent.TransactionId;
    }
    
    public void Apply(TransactionSuccessStateLogEvent transactionSuccessStateLogEvent)
    {
        PendingTransactions.Remove(transactionSuccessStateLogEvent.CreateTransactionGEventId);
    }
    
    public void Apply(TransactionFailedStateLogEvent transactionFailedStateLogEvent)
    {
        PendingTransactions.Remove(transactionFailedStateLogEvent.CreateTransactionGEventId);
    }
}