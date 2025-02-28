using Aevatar.GAgents.GraphRag.Abstractions.Models;

namespace Aevatar.GAgents.GraphRag.Abstractions;

public interface IGraphRagStore
{
    Task StoreAsync(Node node, Relationship relationship);
    // Task<Node> RetrieveAsync(string nodeId);
    Task<IEnumerable<QueryResult>> QueryAsync(string cypherQuery);
    Task DeleteAsync(string nodeId);
}