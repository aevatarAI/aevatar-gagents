using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.AI.Common;

[GenerateSerializer]
public class TokenUsage<T> : StateLogEventBase<T> where T : StateLogEventBase<T>
{
    [Id(0)] public Guid GrainId { get; set; }
    [Id(1)] public int InputToken { get; set; }
    [Id(2)] public int OutputToken { get; set; }
    [Id(3)] public int TotalUsageToken { get; set; }
    [Id(4)] public long CreateTime { get; set; }
}