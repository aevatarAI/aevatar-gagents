namespace Aevatar.GAgents.Neo4jStore.Options;

public sealed class Neo4jDriverConfig
{
    public const string ConfigSectionName = "Neo4j";
    public string Uri { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
}