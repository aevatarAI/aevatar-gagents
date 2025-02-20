using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.Options;

[GenerateSerializer]
public class InitTwitterOptionsDto : ConfigurationBase
{
    [Id(0)] public string ConsumerKey { get; set; }
    [Id(1)] public string ConsumerSecret { get; set; }
    [Id(2)] public string EncryptionPassword { get; set; }
    [Id(3)] public string BearerToken { get; set; }
    [Id(4)] public int ReplyLimit { get; set; }
}