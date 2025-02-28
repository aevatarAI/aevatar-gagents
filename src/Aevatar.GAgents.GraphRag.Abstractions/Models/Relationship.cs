namespace Aevatar.GAgents.GraphRag.Abstractions.Models;

public class Relationship
{
    public string Type { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}