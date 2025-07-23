using System;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.TypeTestAgent.Options;
using Orleans;

namespace Aevatar.GAgents.TypeTestAgent.State;

/// <summary>
/// TypeTestAgent的状态类
/// </summary>
[GenerateSerializer]
public class TypeTestAgentState : AIGAgentStateBase
{
    // 配置相关状态 - 对应ConfigDto的所有属性
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public int MaxCount { get; set; }
    [Id(2)] public bool IsEnabled { get; set; }
    [Id(3)] public double Precision { get; set; }
    [Id(4)] public decimal Price { get; set; }
    [Id(5)] public DateTime StartDate { get; set; }
    [Id(6)] public TimeSpan Duration { get; set; }
    [Id(7)] public TestMode Mode { get; set; }
    [Id(8)] public Guid SessionId { get; set; }
    [Id(9)] public long MaxFileSize { get; set; }
    [Id(10)] public float Threshold { get; set; }
    [Id(11)] public string ApiEndpoint { get; set; } = string.Empty;
    [Id(12)] public ushort Port { get; set; }
    [Id(13)] public byte RetryCount { get; set; }
    [Id(14)] public short ConfigVersion { get; set; }
    
    // 运行时状态
    [Id(15)] public int TestExecutionCount { get; set; }
    [Id(16)] public DateTime LastTestTime { get; set; }
    [Id(17)] public bool IsConfigured { get; set; }
} 