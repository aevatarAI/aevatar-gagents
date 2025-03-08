using System.Text.RegularExpressions;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.GraphRetrievalAgent.Common;
using Aevatar.GAgents.GraphRetrievalAgent.GAgent.SEvent;
using Aevatar.GAgents.GraphRetrievalAgent.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Newtonsoft.Json;

namespace Aevatar.GAgents.GraphRetrievalAgent.GAgent;

public interface IGraphRetrievalAgent : IAIGAgent, IGAgent
{
    Task<string?> InvokeLLMWithGraphRetrievalAsync(string prompt);
}


public class GraphRetrievalAgent : AIGAgentBase<GraphRetrievalAgentState, GraphRetrievalAgentSEvent, EventBase, GraphRetrievalConfig>, IGraphRetrievalAgent
{
    private readonly ILogger<GraphRetrievalAgent> _logger;
    private readonly IDriver _driver;


    public GraphRetrievalAgent(ILogger<GraphRetrievalAgent> logger)
    {
        _logger = logger;
        _driver = ServiceProvider.GetRequiredService<IDriver>();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "This agent enhance llm prompt with graph rag retrieval.");
    }
    
    protected override async Task PerformConfigAsync(GraphRetrievalConfig initializationConfig)
    {
        _logger.LogDebug("PerformConfigAsync , data: {data}",
            JsonConvert.SerializeObject(initializationConfig));
        RaiseEvent(new SetGraphSchemaSEvent
        {
            Schema = initializationConfig.Schema,
            Example = initializationConfig.Example
        });

        await ConfirmEvents();
    }
    
    public async Task<string?> InvokeLLMWithGraphRetrievalAsync(string prompt)
    {
        List<ChatMessage>? history = null;
        var graphRagData = await GraphRagDataAsync(prompt);
        if (!graphRagData.IsNullOrEmpty())
        {
            _logger.LogDebug("add graph rag data: {data}",
                JsonConvert.SerializeObject(graphRagData));
            history = new List<ChatMessage>
            {
                new ChatMessage
                {
                    ChatRole = ChatRole.User,
                    Content = graphRagData
                }
            };
        }
        
        var result = await ChatWithHistory(prompt, history);
        return result?[0].Content;
    }
    
    private async Task<List<QueryResult>> QueryAsync(string cypherQuery)
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
    
    private async Task<string> GraphRagDataAsync(string text)
    {
        var prompt = Prompts.Text2CypherTemplate
            .Replace("{schema}", State.RetrieveSchema)
            .Replace("{examples}", State.RetrieveExample)
            .Replace("{query_text}", text);
        
        var response = await ChatWithHistory(prompt);
        
        if (response.IsNullOrEmpty())
        {
            return string.Empty;
        }
        
        var cypher = response?[0].Content;

        if (cypher.IsNullOrEmpty())
        {
            Logger.LogError("Cannot generate cypher from text: {text}.", text);
            return string.Empty;
        }
        
        cypher = Regex.Replace(cypher, @"<think>.*?</think>", "", RegexOptions.Singleline)
            .TrimStart('\r', '\n')
            .TrimEnd('\r', '\n'); ;
        
        var result = await QueryAsync(cypher);
        if (!result.Any())
        {
            Logger.LogError("query null for cypher: {cypher}.", cypher);
            return string.Empty;
        }
        
        return result.ToNaturalLanguage();
    }
    
    protected override void AIGAgentTransitionState(GraphRetrievalAgentState state,
        StateLogEventBase<GraphRetrievalAgentSEvent> @event)
    {
        switch (@event)
        {
            case SetGraphSchemaSEvent setGraphSchemaSEvent:
                State.RetrieveSchema = setGraphSchemaSEvent.Schema;
                State.RetrieveExample = setGraphSchemaSEvent.Example;
                break;
        }
    }
    
}