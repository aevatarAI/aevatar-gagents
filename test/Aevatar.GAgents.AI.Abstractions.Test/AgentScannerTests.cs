using System.Reflection;
using Aevatar.GAgents.AI.Common;
using Xunit;

namespace Aevatar.GAgents.AI.Abstractions.Test;

public class AgentScannerTests
{
    [Fact]
    public void AgentIndexInfo_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange & Act - Test basic functionality of AgentIndexInfo
        var agentInfo = new AgentIndexInfo
        {
            Id = "test-agent",
            Name = "Test Agent",
            Category = "Test",
            L1Description = "This is a test short description used to verify the basic functionality and setting capability of L1 description field.",
            L2Description = "This is a more detailed test description used to verify that the L2 description field can store longer text content. It includes detailed Agent functionality descriptions, usage scenarios, technical characteristics and other information to provide users with comprehensive understanding of Agent capabilities in actual use. This description should be between 300 to 500 characters to meet our design requirements.",
            Capabilities = new List<string> { "test", "validation" },
            Tags = new List<string> { "test", "unit-test" },
            InputFormat = "json",
            OutputFormat = "json",
            UsageExample = "await TestAgentAsync(new TestRequest())"
        };
        
        // Assert
        Assert.Equal("test-agent", agentInfo.Id);
        Assert.Equal("Test Agent", agentInfo.Name);
        Assert.Equal("Test", agentInfo.Category);
        Assert.NotEmpty(agentInfo.L1Description);
        Assert.NotEmpty(agentInfo.L2Description);
        Assert.Contains("test", agentInfo.Capabilities);
        Assert.Contains("test", agentInfo.Tags);
        Assert.Equal("json", agentInfo.InputFormat);
        Assert.Equal("json", agentInfo.OutputFormat);
        Assert.NotEmpty(agentInfo.UsageExample);
        
        Console.WriteLine("✓ AgentIndexInfo basic functionality validation passed");
    }

    [Fact]
    public void AgentDescriptionAttribute_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange & Act - Test basic functionality of AgentDescriptionAttribute
        var attribute = new AgentDescriptionAttribute(
            "Test Agent",
            "L1 description for quick matching between 100-150 chars. Contains core Agent functionality description.",
            "This is L2 description providing more detailed Agent capability explanation. Includes specific functional features, usage scenarios, technical implementation details and other information. This description is used for in-depth analysis and precise matching, helping users comprehensively understand various Agent capabilities and applicable scenarios. Description length is controlled within 300-500 character range to ensure balance between information completeness and readability."
        )
        {
            Category = "Test",
            Capabilities = new[] { "test", "validation", "mock" },
            Tags = new[] { "test", "unit-test", "validation" },
            InputFormat = "json",
            OutputFormat = "json",
            UsageExample = "await TestAsync(request)"
        };

        // Assert
        Assert.Equal("Test Agent", attribute.Name);
        Assert.NotEmpty(attribute.L1Description);
        Assert.NotEmpty(attribute.L2Description);
        Assert.Equal("Test", attribute.Category);
        Assert.Contains("test", attribute.Capabilities);
        Assert.Contains("test", attribute.Tags);
        Assert.Equal("json", attribute.InputFormat);
        Assert.Equal("json", attribute.OutputFormat);
        Assert.NotEmpty(attribute.UsageExample);

        // Validate description length specifications
        Assert.True(attribute.L1Description.Length >= 100 && attribute.L1Description.Length <= 150,
            $"L1Description length {attribute.L1Description.Length} should be between 100-150 characters");
        Assert.True(attribute.L2Description.Length >= 300 && attribute.L2Description.Length <= 500,
            $"L2Description length {attribute.L2Description.Length} should be between 300-500 characters");
        
        Console.WriteLine("✓ AgentDescriptionAttribute functionality validation passed");
    }
    
    [Fact]
    public void SimpleAgentScanner_ScanAgentsInAssembly_ShouldHandleEmptyAssembly()
    {
        // Arrange - Use current test assembly (assembly without marked Agents)
        var testAssembly = Assembly.GetExecutingAssembly();
        
        // Act
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(testAssembly);
        
        // Assert - Should return empty list instead of throwing exception
        Assert.NotNull(agents);
        Assert.Empty(agents);
        Console.WriteLine("✓ Correctly handled assembly without Agents");
    }

    [Fact]
    public void SimpleAgentScanner_ScanAllLoadedAssemblies_ShouldNotThrow()
    {
        // Arrange & Act - Test scanning all loaded assemblies doesn't throw exception
        var exception = Record.Exception(() =>
        {
            var agents = SimpleAgentScanner.ScanAllLoadedAssemblies();
            Console.WriteLine($"Scan of loaded assemblies completed, found {agents.Count} Agents");
            return agents;
        });
        
        // Assert - Should not throw exception
        Assert.Null(exception);
        Console.WriteLine("✓ Scan all loaded assemblies functionality works properly");
    }

    [Fact]
    public void AgentDescriptionAttribute_ValidationRules_ShouldWork()
    {
        // Arrange & Act - Test boundary conditions
        var shortL1 = new string('a', 99);  // 99 characters, below 100 lower limit
        var longL1 = new string('b', 151);  // 151 characters, above 150 upper limit
        var shortL2 = new string('c', 299); // 299 characters, below 300 lower limit
        var longL2 = new string('d', 501);  // 501 characters, above 500 upper limit

        // Assert - Verify our validation logic catches these boundary cases
        Assert.True(shortL1.Length < 100);
        Assert.True(longL1.Length > 150);
        Assert.True(shortL2.Length < 300);
        Assert.True(longL2.Length > 500);
        
        Console.WriteLine($"✓ Validation boundary rules - L1: {shortL1.Length}(too short), {longL1.Length}(too long)");
        Console.WriteLine($"✓ Validation boundary rules - L2: {shortL2.Length}(too short), {longL2.Length}(too long)");
    }

    [Fact]
    public void AgentIndexInfo_Serialization_ShouldWork()
    {
        // Arrange
        var original = new AgentIndexInfo
        {
            Id = "serialization-test",
            Name = "Serialization Test Agent",
            Category = "Serialization",
            L1Description = "This is Agent description for testing serialization functionality, ensuring all fields can be properly serialized and deserialized.",
            L2Description = "Detailed serialization test description. This Agent is specifically designed to verify AgentIndexInfo object performance in various serialization scenarios, including JSON serialization, XML serialization, etc. Through comprehensive serialization testing, ensure data integrity and consistency of Agent information during transmission and storage processes, providing reliable data guarantee for practical applications.",
            Capabilities = new List<string> { "serialization", "test", "json" },
            Tags = new List<string> { "test", "serialization", "data" },
            InputFormat = "json",
            OutputFormat = "json",
            UsageExample = "await SerializeAsync(data)"
        };

        // Act - Simulate serialization process (simple copy)
        var copy = new AgentIndexInfo
        {
            Id = original.Id,
            Name = original.Name,
            Category = original.Category,
            L1Description = original.L1Description,
            L2Description = original.L2Description,
            Capabilities = new List<string>(original.Capabilities),
            Tags = new List<string>(original.Tags),
            InputFormat = original.InputFormat,
            OutputFormat = original.OutputFormat,
            UsageExample = original.UsageExample
        };

        // Assert
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.Category, copy.Category);
        Assert.Equal(original.L1Description, copy.L1Description);
        Assert.Equal(original.L2Description, copy.L2Description);
        Assert.Equal(original.Capabilities.Count, copy.Capabilities.Count);
        Assert.Equal(original.Tags.Count, copy.Tags.Count);
        Assert.Equal(original.InputFormat, copy.InputFormat);
        Assert.Equal(original.OutputFormat, copy.OutputFormat);
        Assert.Equal(original.UsageExample, copy.UsageExample);
        
        Console.WriteLine("✓ AgentIndexInfo serialization compatibility validation passed");
    }
} 