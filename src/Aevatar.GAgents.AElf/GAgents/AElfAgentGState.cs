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
    
    [Id(1)] public Dictionary<Guid, CreateTransactionSEvent> PendingTransactions { get; set; } = new Dictionary<Guid, CreateTransactionSEvent>();
    
    public void Apply(CreateTransactionSEvent createTransactionSEvent)
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }
        PendingTransactions[createTransactionSEvent.Id] = createTransactionSEvent;
    }
    
    public void Apply(SendTransactionSEvent sendTransactionSEvent)
    {
        PendingTransactions[sendTransactionSEvent.CreateTransactionGEventId].TransactionId =
            sendTransactionSEvent.TransactionId;
    }
    
    public void Apply(TransactionSuccessSEvent transactionSuccessSEvent)
    {
        PendingTransactions.Remove(transactionSuccessSEvent.CreateTransactionGEventId);
    }
    
    public void Apply(TransactionFailedSEvent transactionFailedSEvent)
    {
        PendingTransactions.Remove(transactionFailedSEvent.CreateTransactionGEventId);
    }
}