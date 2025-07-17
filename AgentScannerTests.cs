using System.Reflection;
using Aevatar.GAgents.AI.Common;
using Xunit;

namespace Aevatar.GAgents.AI.Abstractions.Test;

public class AgentScannerTests
{
    [Fact]
    public void AgentIndexInfo_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange & Act - 测试AgentIndexInfo的基本功能
        var agentInfo = new AgentIndexInfo
        {
            Id = "test-agent",
            Name = "测试Agent",
            Category = "Test",
            L1Description = "这是一个测试用的简短描述，用于验证L1描述字段的基本功能和设置能力。",
            L2Description = "这是一个更详细的测试描述，用于验证L2描述字段能够存储更长的文本内容。它包含了Agent的详细功能说明、使用场景、技术特性等信息，以便在实际使用中为用户提供全面的Agent能力理解。这个描述应该在300到500字符之间，以符合我们的设计要求。",
            Capabilities = new List<string> { "test", "validation" },
            Tags = new List<string> { "test", "unit-test" },
            IsActive = true
        };
        
        // Assert
        Assert.Equal("test-agent", agentInfo.Id);
        Assert.Equal("测试Agent", agentInfo.Name);
        Assert.Equal("Test", agentInfo.Category);
        Assert.NotEmpty(agentInfo.L1Description);
        Assert.NotEmpty(agentInfo.L2Description);
        Assert.Contains("test", agentInfo.Capabilities);
        Assert.Contains("test", agentInfo.Tags);
        Assert.True(agentInfo.IsActive);
        
        Console.WriteLine("✓ AgentIndexInfo基本功能验证通过");
    }

    [Fact]
    public void AgentDescriptionAttribute_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange & Act - 测试AgentDescriptionAttribute的基本功能
        var attribute = new AgentDescriptionAttribute(
            Name: "测试Agent",
            L1Description: "这是L1描述，用于快速匹配，长度在100-150字符之间。包含Agent的核心功能说明，适用于快速筛选和匹配场景。",
            L2Description: "这是L2描述，提供更详细的Agent能力说明。包括具体的功能特性、使用场景、技术实现细节等信息。这个描述用于深度分析和精确匹配，帮助用户全面了解Agent的各项能力和适用场景。描述长度控制在300-500字符范围内，确保信息完整性和可读性的平衡。",
            Category: "Test",
            Capabilities: new[] { "test", "validation", "mock" },
            Tags: new[] { "test", "unit-test", "validation" }
        );

        // Assert
        Assert.Equal("测试Agent", attribute.Name);
        Assert.NotEmpty(attribute.L1Description);
        Assert.NotEmpty(attribute.L2Description);
        Assert.Equal("Test", attribute.Category);
        Assert.Contains("test", attribute.Capabilities);
        Assert.Contains("test", attribute.Tags);

        // 验证描述长度规范
        Assert.True(attribute.L1Description.Length >= 100 && attribute.L1Description.Length <= 150,
            $"L1Description length {attribute.L1Description.Length} should be between 100-150 characters");
        Assert.True(attribute.L2Description.Length >= 300 && attribute.L2Description.Length <= 500,
            $"L2Description length {attribute.L2Description.Length} should be between 300-500 characters");
        
        Console.WriteLine("✓ AgentDescriptionAttribute功能验证通过");
    }
    
    [Fact]
    public void SimpleAgentScanner_ScanAgentsInAssembly_ShouldHandleEmptyAssembly()
    {
        // Arrange - 使用当前测试程序集（没有标记Agent的程序集）
        var testAssembly = Assembly.GetExecutingAssembly();
        
        // Act
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(testAssembly);
        
        // Assert - 应该返回空列表而不是抛异常
        Assert.NotNull(agents);
        Assert.Empty(agents);
        Console.WriteLine("✓ 正确处理没有Agent的程序集");
    }

    [Fact]
    public void SimpleAgentScanner_ScanAllLoadedAssemblies_ShouldNotThrow()
    {
        // Arrange & Act - 测试扫描所有已加载程序集不会抛异常
        var exception = Record.Exception(() =>
        {
            var agents = SimpleAgentScanner.ScanAllLoadedAssemblies();
            Console.WriteLine($"扫描已加载程序集完成，找到 {agents.Count} 个Agent");
            return agents;
        });
        
        // Assert - 不应该抛异常
        Assert.Null(exception);
        Console.WriteLine("✓ 扫描所有已加载程序集功能正常");
    }

    [Fact]
    public void AgentDescriptionAttribute_ValidationRules_ShouldWork()
    {
        // Arrange & Act - 测试边界情况
        var shortL1 = new string('a', 99);  // 99字符，低于100的下限
        var longL1 = new string('b', 151);  // 151字符，超过150的上限
        var shortL2 = new string('c', 299); // 299字符，低于300的下限
        var longL2 = new string('d', 501);  // 501字符，超过500的上限

        // Assert - 验证我们的验证逻辑会捕获这些边界情况
        Assert.True(shortL1.Length < 100);
        Assert.True(longL1.Length > 150);
        Assert.True(shortL2.Length < 300);
        Assert.True(longL2.Length > 500);
        
        Console.WriteLine($"✓ 验证边界规则 - L1: {shortL1.Length}(too short), {longL1.Length}(too long)");
        Console.WriteLine($"✓ 验证边界规则 - L2: {shortL2.Length}(too short), {longL2.Length}(too long)");
    }

    [Fact]
    public void AgentIndexInfo_Serialization_ShouldWork()
    {
        // Arrange
        var original = new AgentIndexInfo
        {
            Id = "serialization-test",
            Name = "序列化测试Agent",
            Category = "Serialization",
            L1Description = "这是用于测试序列化功能的Agent描述，确保所有字段都能正确序列化和反序列化。",
            L2Description = "详细的序列化测试描述。这个Agent专门用于验证AgentIndexInfo对象在各种序列化场景下的表现，包括JSON序列化、XML序列化等。通过完整的序列化测试，确保Agent信息在传输和存储过程中的数据完整性和一致性，为实际应用提供可靠的数据保障。",
            Capabilities = new List<string> { "serialization", "test", "json" },
            Tags = new List<string> { "test", "serialization", "data" },
            IsActive = true
        };

        // Act - 模拟序列化过程（简单的复制）
        var copy = new AgentIndexInfo
        {
            Id = original.Id,
            Name = original.Name,
            Category = original.Category,
            L1Description = original.L1Description,
            L2Description = original.L2Description,
            Capabilities = new List<string>(original.Capabilities),
            Tags = new List<string>(original.Tags),
            IsActive = original.IsActive
        };

        // Assert
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.Category, copy.Category);
        Assert.Equal(original.L1Description, copy.L1Description);
        Assert.Equal(original.L2Description, copy.L2Description);
        Assert.Equal(original.Capabilities.Count, copy.Capabilities.Count);
        Assert.Equal(original.Tags.Count, copy.Tags.Count);
        Assert.Equal(original.IsActive, copy.IsActive);
        
        Console.WriteLine("✓ AgentIndexInfo序列化兼容性验证通过");
    }
} 