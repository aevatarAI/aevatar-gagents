using Aevatar.GAgents.GraphRag.Abstractions;
using Xunit;

namespace Aevatar.GAgents.GraphRag.Test;

public class Neo4jStoreTest : AevatarGAgentGraphRagTestBase
{
    private IGraphRagStore _neo4jStore;
    
    public Neo4jStoreTest()
    {
        _neo4jStore = GetRequiredService<IGraphRagStore>();;
    }

    [Fact]
    public async Task CypherQueryTest()
    {
        var cypherQuery =
            @"
      MATCH
  (h:Hashtag)<-[:TAGS]-(t:Tweet)<-[:POSTS]-(u:User:Me)
WITH 
  h, COUNT(h) AS Hashtags
ORDER BY 
  Hashtags DESC
LIMIT 10
RETURN 
  h.name, Hashtags
      ";
        
        var result = await _neo4jStore.QueryAsync(cypherQuery);
        Assert.True(result.Any());
    }
}