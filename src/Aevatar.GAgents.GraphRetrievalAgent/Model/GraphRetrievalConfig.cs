using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.GraphRetrievalAgent.Model;

[GenerateSerializer]
public class GraphRetrievalConfig : ConfigurationBase
{
    [Id(0)]
    [DefaultValues("")]
    public string Schema { get; set; } = string.Empty;
    
    [Id(1)]
    [DefaultValues("")]
    public string Example { get; set; } = string.Empty;
    
    [Id(2)]
    [DefaultValues(10, 5, 20, 50)]
    public int MaxResults { get; set; } = 10;
    
    [Id(3)]
    [DefaultValues(0.8, 0.7, 0.9)]
    public double SimilarityThreshold { get; set; } = 0.8;
    
    [Id(4)]
    [DefaultValues(3, 2, 5)]
    public int MaxDepth { get; set; } = 3;
    
    [Id(5)]
    [DefaultValues(true)]
    public bool EnableSemanticSearch { get; set; } = true;
}