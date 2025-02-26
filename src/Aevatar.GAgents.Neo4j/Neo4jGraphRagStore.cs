using Aevatar.GAgents.GraphRag.Abstractions;
using Aevatar.GAgents.GraphRag.Abstractions.Models;
using Neo4j.Driver;

namespace Aevatar.GAgents.Neo4j;

public class Neo4jGraphRagStore : IGraphRagStore
{
    private readonly IDriver _driver;

    public Neo4jGraphRagStore(IDriver driver)
    {
        _driver = driver;
    }
    
    public async Task StoreAsync(Node node, Relationship relationship)
    {
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var parameters = new
            {
                nodeId = node.Id,
                labels = node.Labels,
                nodeProps = node.Properties,
                relType = relationship.Type,
                relProps = relationship.Properties
            };

            var query = @"
            MERGE (n:Node {id: $nodeId})
            SET n += $nodeProps
            FOREACH (label IN $labels | 
                MERGE (n)-[:HAS_LABEL]->(:Label {name: label})
            )
            MERGE (n)-[r:RELATIONSHIP {type: $relType}]->(m)
            SET r += $relProps";

            await tx.RunAsync(query, parameters);
        });
    }

    // public async Task<Node> RetrieveAsync(string nodeId)
    // {
    //     await using var session = _driver.AsyncSession();
    //     return await session.ExecuteReadAsync(async tx =>
    //     {
    //         var result = await tx.RunAsync(
    //             "MATCH (n {id: $id}) RETURN n",
    //             new { id = nodeId });
    //
    //         var record = await result.SingleAsync();
    //         var node = record["n"].As<INode>();
    //         
    //         return new Node
    //         {
    //             Id = node.Properties["id"].ToString(),
    //             Labels = (List<string>)node.Labels,
    //             Properties = node.Properties.ToDictionary()
    //         };
    //     });
    // }

    public async Task<IEnumerable<QueryResult>> QueryAsync(string cypherQuery)
    {
        await using var session = _driver.AsyncSession();
        return await session.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync(cypherQuery);
            return await result.ToListAsync(r => new QueryResult
            {
                Data = r.Values.ToDictionary(
                    k => k.Key,
                    v => (object)v.Value)
            });
        });
    }

    public async Task DeleteAsync(string nodeId)
    {
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(
                "MATCH (n {id: $id}) DETACH DELETE n",
                new { id = nodeId });
        });
    }
}