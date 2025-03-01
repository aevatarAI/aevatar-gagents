using Aevatar.GAgents.GraphRag.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Aevatar.GAgents.Neo4jStore.GraphRagStore;

public class Neo4JStore : INeo4JStore
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4JStore> _logger;

    public Neo4JStore(
        IDriver driver, 
        ILogger<Neo4JStore> logger)
    {
        _driver = driver;
        _logger = logger;
    }
    
    public async Task StoreAsync(Node node, Relationship relationship)
    {
        try
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
        catch (ClientException e)
        {
            _logger.LogError("Error storing, msg: {msg}, code: {code}", e.Message, e.Code);
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Error authentication, msg: {msg}, code: {code}", e.Message, e.Code);
        }
    }

    public async Task<Node?> RetrieveAsync(string nodeId)
    {
        try
        {
            await using var session = _driver.AsyncSession();
            return await session.ExecuteReadAsync(async tx =>
            {
                var result = await tx.RunAsync(
                    "MATCH (n {id: $id}) RETURN n",
                    new { id = nodeId });

                var record = await result.SingleAsync();
                var node = ValueExtensions.As<INode>(record["n"]);

                return new Node
                {
                    Id = node.Properties["id"].ToString(),
                    Labels = (List<string>)node.Labels,
                    Properties = node.Properties.ToDictionary()
                };
            });
        }
        catch (ClientException e)
        {
            _logger.LogError("Error Retrieving, msg: {msg}, code: {code}", e.Message, e.Code);
            return null;
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Error authentication, msg: {msg}, code: {code}", e.Message, e.Code);
            return null;
        }
    }

    public async Task<List<QueryResult>> QueryAsync(string cypherQuery)
    {
        try
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
        catch (ClientException e)
        {
            _logger.LogError("Error executing Cypher query, msg: {msg}, code: {code}", e.Message, e.Code);
            return new List<QueryResult>();
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Error authentication, msg: {msg}, code: {code}", e.Message, e.Code);
            return new List<QueryResult>();
        }
    }

    public async Task DeleteAsync(string nodeId)
    {
        try
        {
            await using var session = _driver.AsyncSession();
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(
                    "MATCH (n {id: $id}) DETACH DELETE n",
                    new { id = nodeId });
            });
        }
        catch (ClientException e)
        {
            _logger.LogError("Error Deleting, msg: {msg}, code: {code}", e.Message, e.Code);
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Error authentication, msg: {msg}, code: {code}", e.Message, e.Code);
        }
        
    }
}