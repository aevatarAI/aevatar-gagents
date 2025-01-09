using System;
using Orleans;

namespace Aevatar.GAgents.Twitter.Grains;

[GenerateSerializer]
public class TwitterState
{
    [Id(0)] public  Guid Id { get; set; }
}