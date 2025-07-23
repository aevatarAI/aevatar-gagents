using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Orleans;

namespace Aevatar.GAgents.TypeTestAgent.Options;

/// <summary>
/// 用于测试各种数据类型的配置DTO
/// </summary>
[GenerateSerializer]
public class TypeTestConfigDto : ConfigurationBase
{
    // 1. String类型
    [Id(0)]
    public string Name { get; set; } = "Default Test Name";
    
    // 2. Int类型
    [Id(1)]
    public int MaxCount { get; set; } = 100;
    
    // 3. Bool类型
    [Id(2)]
    public bool IsEnabled { get; set; } = true;
    
    // 4. Double类型
    [Id(3)]
    public double Precision { get; set; } = 3.14159;
    
    // 5. Decimal类型（金额）
    [Id(4)]
    public decimal Price { get; set; } = 99.99m;
    
    // 6. DateTime类型
    [Id(5)]
    public DateTime StartDate { get; set; } = new DateTime(2024, 1, 1);
    
    // 7. TimeSpan类型（持续时间）
    [Id(6)]
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);
    
    // 8. Enum类型
    [Id(7)]
    public TestMode Mode { get; set; } = TestMode.Development;
    
    // 9. Guid类型
    [Id(8)]
    public Guid SessionId { get; set; } = new Guid("550e8400-e29b-41d4-a716-446655440000");
    
    // 10. Long类型
    [Id(9)]
    public long MaxFileSize { get; set; } = 1000000L;
    
    // 11. Float类型
    [Id(10)]
    public float Threshold { get; set; } = 0.95f;
    
    // 12. String URL类型
    [Id(11)]
    public string ApiEndpoint { get; set; } = "https://api.example.com";
    
    // 13. 端口号（ushort）
    [Id(12)]
    public ushort Port { get; set; } = 8080;
    
    // 14. 重试次数（byte）
    [Id(13)]
    public byte RetryCount { get; set; } = 3;
    
    // 15. 配置版本（short）
    [Id(14)]
    public short ConfigVersion { get; set; } = 1;
}

/// <summary>
/// 测试模式枚举
/// </summary>
public enum TestMode
{
    Development = 1,
    Testing = 2,
    Staging = 3,
    Production = 4
} 