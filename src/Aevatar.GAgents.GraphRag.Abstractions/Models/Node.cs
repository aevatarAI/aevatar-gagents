namespace Aevatar.GAgents.GraphRag.Abstractions.Models;

public class Node
{
    public string Id { get; set; }
    public List<string> Labels { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}