using Neo4j.Driver;
using Xunit;

namespace Aevatar.GAgents.GraphRag.Test;

public class Neo4jTest : AevatarGAgentGraphRagTestBase
{
    private IDriver _driver;
    
    public Neo4jTest()
    {
        _driver = GetRequiredService<IDriver>();;
    }

    [Fact]
    public async Task RetrieveTest()
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

        var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));
        var result = await session.ReadTransactionAsync(async tx =>
        {
            var r = await tx.RunAsync(cypherQuery,
                new { screenName = "NASA" });
            return await r.ToListAsync();
        });

        await session.CloseAsync();
    }
}