using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GraphRetrievalAgent.Model;

[GenerateSerializer]
public class GraphRetrievalConfig : ConfigurationBase
{
    [Id(0)] public string Schema { get; set; } = string.Empty;
    [Id(1)] public string Example { get; set; } = string.Empty;
}