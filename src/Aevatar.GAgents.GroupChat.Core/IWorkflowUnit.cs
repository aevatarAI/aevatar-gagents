namespace Aevatar.GAgents.GroupChat.Core;

public interface IWorkflowUnit
{
    /// <summary>
    /// Establish relationships with other workflow units
    /// </summary>
    /// <param name="relatedUnit">Identifier of the related workflow unit</param>
    /// <param name="relationship">Type of relationship (e.g., upstream, downstream, resource_provider)</param>
    Task EstablishRelationshipAsync(GrainId relatedUnit, string relationship);

    /// <summary>
    /// Get the capability description of this workflow unit
    /// </summary>
    Task<WorkflowUnitCapabilities> GetCapabilitiesAsync();

    /// <summary>
    /// Prepare to execute workflow tasks
    /// </summary>
    /// <param name="context">Workflow execution context</param>
    Task PrepareForExecutionAsync(WorkflowExecutionContext context);
}

/// <summary>
/// Workflow unit capability description
/// </summary>
[GenerateSerializer]
public class WorkflowUnitCapabilities
{
    [Id(0)] public string UnitType { get; set; } = "Basic";
    [Id(1)] public List<string> ProvidedCapabilities { get; set; } = new();
    [Id(2)] public List<string> RequiredCapabilities { get; set; } = new();
    [Id(3)] public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Workflow execution context
/// </summary>
[GenerateSerializer]
public class WorkflowExecutionContext
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public List<GrainId> AvailableResources { get; set; } = new();
    [Id(2)] public Dictionary<string, object> SharedData { get; set; } = new();
}