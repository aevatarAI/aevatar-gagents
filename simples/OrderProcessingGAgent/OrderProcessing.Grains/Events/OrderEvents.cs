using System;
using Aevatar.Core.Abstractions;
using OrderProcessing.Grains.Dto;

namespace OrderProcessing.Grains.Events;

[GenerateSerializer]
public class OrderCreatedEvent : EventBase
{
    [Id(0)]
    public OrderDto Order { get; set; } = new();
}

[GenerateSerializer]
public class OrderValidationRequestEvent : EventBase
{
    [Id(0)]
    public OrderDto Order { get; set; } = new();
}

[GenerateSerializer]
public class OrderValidationCompletedEvent : EventBase
{
    [Id(0)]
    public OrderDto Order { get; set; } = new();
    
    [Id(1)]
    public OrderValidationResult ValidationResult { get; set; } = new();
}

[GenerateSerializer]
public class StartWorkflowEvent : EventBase
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public WorkflowConfigDto WorkflowConfig { get; set; } = new();
}

[GenerateSerializer]
public class WorkflowStepCompletedEvent : EventBase
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public string CompletedStep { get; set; } = string.Empty;
    
    [Id(2)]
    public string NextStep { get; set; } = string.Empty;
    
    [Id(3)]
    public bool IsSuccess { get; set; }
    
    [Id(4)]
    public string Message { get; set; } = string.Empty;
}

[GenerateSerializer]
public class WorkflowCompletedEvent : EventBase
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public OrderStatus FinalStatus { get; set; }
    
    [Id(2)]
    public string Message { get; set; } = string.Empty;
} 