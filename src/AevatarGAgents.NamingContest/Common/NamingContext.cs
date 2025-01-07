using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.NamingContest.Common;

[GenerateSerializer]
public class NamingContext:EventBase
{
    [Id(0)]public string Content { get; set; }
}