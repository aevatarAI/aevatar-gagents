namespace Aevatar.GAgents.Neo4j.Common;

public class Prompts
{
    public const string Text2CypherTemplate = 
        @"
Task: Generate a Cypher statement for querying a Neo4j graph database from a user input.

Schema:
{schema}

Examples (optional):
{examples}

Input:
{query_text}

Do not use any properties or relationships not included in the schema.
Do not include triple backticks ``` or any additional text except the generated Cypher statement in your response.

Cypher query:
";
}