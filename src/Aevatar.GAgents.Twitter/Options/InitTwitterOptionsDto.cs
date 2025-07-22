using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Orleans;

namespace Aevatar.GAgents.Twitter.Options;

[GenerateSerializer]
public class InitTwitterOptionsDto : ConfigurationBase
{
    [Id(0)]
    [DefaultValues("YOUR_TWITTER_API_KEY")]
    public string ConsumerKey { get; set; }
    
    [Id(1)]
    [DefaultValues("YOUR_API_SECRET")]
    public string ConsumerSecret { get; set; }
    
    [Id(2)]
    [DefaultValues("YOUR_ENCRYPTION_PASSWORD")]
    public string EncryptionPassword { get; set; }
    
    [Id(3)]
    [DefaultValues("YOUR_BEARER_TOKEN")]
    public string BearerToken { get; set; }
    
    [Id(4)]
    [DefaultValues(10, 5, 20, 50)]
    public int ReplyLimit { get; set; }
}