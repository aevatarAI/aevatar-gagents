using System;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TypeTestAgent.Options;
using Orleans;

namespace Aevatar.GAgents.TypeTestAgent.Events;

/// <summary>
/// TypeTest配置设置事件
/// </summary>
[GenerateSerializer]
public class TypeTestConfigSEvent : StateLogEventBase<TypeTestEvent>
{
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
}

/// <summary>
/// 测试执行事件
/// </summary>
[GenerateSerializer]
public class TestExecutionSEvent : StateLogEventBase<TypeTestEvent>
{
    [Id(0)] public DateTime ExecutionTime { get; set; }
    [Id(1)] public string TestType { get; set; } = string.Empty;
    [Id(2)] public bool IsSuccess { get; set; }
    [Id(3)] public string Result { get; set; } = string.Empty;
}

/// <summary>
/// 配置状态更新事件
/// </summary>
[GenerateSerializer] 
public class ConfigurationStatusSEvent : StateLogEventBase<TypeTestEvent>
{
    [Id(0)] public bool IsConfigured { get; set; }
    [Id(1)] public DateTime ConfiguredTime { get; set; }
}

/// <summary>
/// TypeTest事件基类
/// </summary>
[GenerateSerializer]
public abstract class TypeTestEvent : StateLogEventBase<TypeTestEvent>
{
} 