using System.Collections.Generic;
using Newtonsoft.Json;

namespace Aevatar.GAgents.VectorStorage.Dto;

public class CollectionsResponse
{
    [JsonProperty("result")]
    public CollectionResult Result { get; set; }
}
    
public class CollectionResult
{
    [JsonProperty("collections")]
    public List<CollectionItem> Collections { get; set; }
}
    
public class CollectionItem
{
    [JsonProperty("name")]
    public string Name { get; set; }
}

public class SearchResponse
{
    public List<Result> Result { get; set; }
}

public class Result
{
    public Payload Payload { get; set; }
}

public class Payload
{
    public string Text { get; set; }
}