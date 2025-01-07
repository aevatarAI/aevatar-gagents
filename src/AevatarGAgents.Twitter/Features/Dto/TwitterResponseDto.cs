using System.Collections.Generic;
using Newtonsoft.Json;
using Orleans;

namespace AevatarGAgents.Twitter.Dto;

[GenerateSerializer]
public class Tweet
{
    
    [JsonProperty("text")]
    public string Text { get; set; }
    
    [JsonProperty("id")]
    public string Id { get; set; }
}

public class Meta
{
    [JsonProperty("result_count")]
    public int ResultCount { get; set; }
    
    [JsonProperty("newest_id")]
    public string NewestId { get; set; }
    
    [JsonProperty("oldest_id")]
    public string OldestId { get; set; }
}

public class TwitterResponseDto
{
    [JsonProperty("data")]
    public List<Tweet> Data { get; set; }
    
    [JsonProperty("meta")]
    public Meta Meta { get; set; }
}
