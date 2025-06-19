using System;

namespace OrderProcessing.Grains.Dto;

[GenerateSerializer]
public class OrderDto
{
    [Id(0)]
    public Guid OrderId { get; set; }
    
    [Id(1)]
    public string CustomerName { get; set; } = string.Empty;
    
    [Id(2)]
    public decimal Amount { get; set; }
    
    [Id(3)]
    public string ProductName { get; set; } = string.Empty;
    
    [Id(4)]
    public int Quantity { get; set; }
    
    [Id(5)]
    public DateTime CreateTime { get; set; }
    
    [Id(6)]
    public OrderStatus Status { get; set; }
}

[GenerateSerializer]
public enum OrderStatus
{
    [Id(0)]
    Created,
    [Id(1)]
    Validated,
    [Id(2)]
    Approved,
    [Id(3)]
    Processing,
    [Id(4)]
    Shipped,
    [Id(5)]
    Completed,
    [Id(6)]
    Rejected
}

[GenerateSerializer]
public class OrderValidationResult
{
    [Id(0)]
    public bool IsValid { get; set; }
    
    [Id(1)]
    public string Message { get; set; } = string.Empty;
    
    [Id(2)]
    public int ConfidenceScore { get; set; }
}

[GenerateSerializer]
public class WorkflowConfigDto
{
    [Id(0)]
    public Guid WorkflowId { get; set; }
    
    [Id(1)]
    public string WorkflowName { get; set; } = string.Empty;
    
    [Id(2)]
    public List<WorkflowStepDto> Steps { get; set; } = new();
}

[GenerateSerializer]
public class WorkflowStepDto
{
    [Id(0)]
    public string StepName { get; set; } = string.Empty;
    
    [Id(1)]
    public string AgentId { get; set; } = string.Empty;
    
    [Id(2)]
    public string NextStepName { get; set; } = string.Empty;
    
    [Id(3)]
    public int Priority { get; set; }
} 