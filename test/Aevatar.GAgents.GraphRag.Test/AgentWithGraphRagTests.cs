using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Aevatar.GAgents.SocialChat.GAgent;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.GraphRag.Test;

public class AgentWithGraphRagTest : AevatarGAgentGraphRagTestBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ITestOutputHelper _outputHelper;

    public AgentWithGraphRagTest(ITestOutputHelper outputHelper)
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _outputHelper = outputHelper;
    }


    [Fact]
    public async Task TwitterDataSetRetrievalTest()
    {
        var agentWithGraphRag = await _gAgentFactory.GetGAgentAsync<ISocialGAgent>(Guid.NewGuid());
        await agentWithGraphRag.InitializeAsync(new InitializeDto()
        {
            Instructions = "answer user questions",
            LLM = "AzureOpenAI",
        });
        
        var agentWithoutGraphRag = await _gAgentFactory.GetGAgentAsync<ISocialGAgent>(Guid.NewGuid());
        await agentWithoutGraphRag.InitializeAsync(new InitializeDto()
        {
            Instructions = "answer user questions",
            LLM = "AzureOpenAI",
        });
        
        var schema = """
                     Node properties:
                     Tweet {id, created_at, id_str, text, favorites, import_method}
                     User:Me {screen_name, name, location, followers, following, url, profile_image_url}
                     Hashtag {name}
                     Link {url}
                     Source {name}
                     User {screen_name, name, location, followers, following, statuses, url, profile_image_url}

                     Relationship properties:
                     FOLLOWS {}
                     POSTS {}
                     USING {}
                     TAGS {}
                     CONTAINS {}
                     MENTIONS {}
                     RETWEETS {}
                     REPLY_TO {}

                     The relationships:
                     (:User:Me)-[:FOLLOWS]->(:User)
                     (:User)-[:FOLLOWS]->(:User:Me)
                     (:User:Me)-[:POSTS]->(:Tweet)
                     (:User)-[:POSTS]->(:Tweet)
                     (:Tweet)-[:USING]->(:Source)
                     (:Tweet)-[:TAGS]->(:Hashtag)
                     (:Tweet)-[:CONTAINS]->(:Link)
                     (:Tweet)-[:MENTIONS]->(:User)
                     (:Tweet)-[:MENTIONS]->(:User:Me)
                     (:Tweet)-[:RETWEETS]->(:Tweet)
                     (:Tweet)-[:REPLY_TO]->(:Tweet)
                     """;

        var example =
            "USER INPUT: 'Who is the most active user?' QUERY: MATCH (u:User)-[:POSTS]->(t:Tweet) RETURN u.screen_name, COUNT(t) AS tweetCount";

        await agentWithGraphRag.SetGraphRagRetrieveInfo(schema, example);

        var question = "recommend some topic I might be interested in twitter?";
        var response1 = await agentWithoutGraphRag.ChatAsync(question);
        _outputHelper.WriteLine("response from agent without graph rag");
        _outputHelper.WriteLine(response1?[0].Content);
        
        _outputHelper.WriteLine("\n \n \n");
        
        var response2 = await agentWithGraphRag.ChatAsync(question);
        _outputHelper.WriteLine("response from agent with graph rag");
        _outputHelper.WriteLine(response2?[0].Content);
    }
    
    
    [Fact]
    public async Task MovieDataSetRetrievalTest()
    {
        var agentWithGraphRag = await _gAgentFactory.GetGAgentAsync<ISocialGAgent>(Guid.NewGuid());
        await agentWithGraphRag.InitializeAsync(new InitializeDto()
        {
            Instructions = "answer user questions",
            LLM = "AzureOpenAI",
        });
        
        var agentWithoutGraphRag = await _gAgentFactory.GetGAgentAsync<ISocialGAgent>(Guid.NewGuid());
        await agentWithoutGraphRag.InitializeAsync(new InitializeDto()
        {
            Instructions = "answer user questions",
            LLM = "AzureOpenAI",
        });
        
        var schema = """
                     Node properties:
                     Person {name: STRING, born: INTEGER}
                     Movie {tagline: STRING, title: STRING, released: INTEGER}
                     
                     Relationship properties:
                     ACTED_IN {roles: LIST}
                     REVIEWED {summary: STRING, rating: INTEGER}
                     
                     The relationships:
                     (:Person)-[:ACTED_IN]->(:Movie)
                     (:Person)-[:DIRECTED]->(:Movie)
                     (:Person)-[:PRODUCED]->(:Movie)
                     (:Person)-[:WROTE]->(:Movie)
                     (:Person)-[:FOLLOWS]->(:Person)
                     (:Person)-[:REVIEWED]->(:Movie)
                     """;

        var example =
            "USER INPUT: 'Which actors starred in the Matrix?' QUERY: MATCH (p:Person)-[:ACTED_IN]->(m:Movie) WHERE m.title = 'The Matrix' RETURN p.name";

        await agentWithGraphRag.SetGraphRagRetrieveInfo(schema, example);

        var question = "I like the Cloud Atlas movie, recommend me some movies like that?";
        var response1 = await agentWithoutGraphRag.ChatAsync(question);
        _outputHelper.WriteLine("response from agent without graph rag");
        _outputHelper.WriteLine(response1?[0].Content);
        
        _outputHelper.WriteLine("\n \n \n");
        
        var response2 = await agentWithGraphRag.ChatAsync(question);
        _outputHelper.WriteLine("response from agent with graph rag");
        _outputHelper.WriteLine(response2?[0].Content);
    }
}