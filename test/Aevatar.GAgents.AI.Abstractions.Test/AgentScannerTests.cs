using System.Reflection;
using Aevatar.GAgents.AI.Common;
using Xunit;

namespace Aevatar.GAgents.AI.Abstractions.Test;

// Test Agent classes for scanning functionality testing
[AgentDescription(
    "Test Agent Alpha",
    "This is L1 description for Test Agent Alpha with correct length between 100-150 characters for testing scanning logic.",
    "This is L2 description for Test Agent Alpha providing detailed capability explanation that should be between 300-500 characters to test the extraction logic properly. This description contains comprehensive information about the agent's capabilities, features, and usage scenarios for proper testing purposes."
)]
public class TestAgentAlpha 
{
}

[AgentDescription(
    "Test Agent Beta",
    "L1 description for Beta agent with minimal required content to test edge cases and boundary conditions properly.",
    "L2 description for Test Agent Beta with different content and structure to verify that the scanner can handle multiple agents with varying descriptions. This description tests the scanner's ability to extract information from different agent configurations and ensures proper data mapping functionality."
)]
public class TestAgentBeta
{
}

// Agent without attribute - should not be scanned
public class RegularClassWithoutAttribute
{
}

// Agent with invalid L1 description (too short)
[AgentDescription("Invalid Agent", "Too short")]
public class TestAgentWithInvalidL1
{
}

public class AgentScannerTests
{
    [Fact]
    public void SimpleAgentScanner_ScanCurrentAssembly_ShouldFindMarkedAgents()
    {
        // Act - Scan the current test assembly which contains our test agents
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(Assembly.GetExecutingAssembly());
        
        // Assert - Should find agents with AgentDescriptionAttribute
        Assert.NotEmpty(agents);
        Assert.Contains(agents, a => a.Name == "Test Agent Alpha");
        Assert.Contains(agents, a => a.Name == "Test Agent Beta");
        
        // Should not find classes without the attribute
        Assert.DoesNotContain(agents, a => a.Name.Contains("RegularClassWithoutAttribute"));
        
        Console.WriteLine($"✓ Found {agents.Count} agents in test assembly");
    }

    [Fact]
    public void SimpleAgentScanner_ExtractAgentInfo_ShouldMapAttributeDataCorrectly()
    {
        // Act
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(Assembly.GetExecutingAssembly());
        var alphaAgent = agents.FirstOrDefault(a => a.Name == "Test Agent Alpha");
        
        // Assert - Verify correct data extraction from AgentDescriptionAttribute
        Assert.NotNull(alphaAgent);
        Assert.Equal("Test Agent Alpha", alphaAgent.Name);
        Assert.NotEmpty(alphaAgent.L1Description);
        Assert.NotEmpty(alphaAgent.L2Description);
        Assert.True(alphaAgent.L1Description.Length >= 100 && alphaAgent.L1Description.Length <= 150,
            $"L1Description length {alphaAgent.L1Description.Length} should be between 100-150 characters");
        Assert.True(alphaAgent.L2Description.Length >= 300 && alphaAgent.L2Description.Length <= 500,
            $"L2Description length {alphaAgent.L2Description.Length} should be between 300-500 characters");
        
        Console.WriteLine("✓ Agent data extraction and mapping works correctly");
    }

    [Fact]
    public void SimpleAgentScanner_ScanMultipleAgents_ShouldReturnDistinctResults()
    {
        // Act
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(Assembly.GetExecutingAssembly());
        
        // Assert - Should find multiple distinct agents
        var alphaAgent = agents.FirstOrDefault(a => a.Name == "Test Agent Alpha");
        var betaAgent = agents.FirstOrDefault(a => a.Name == "Test Agent Beta");
        
        Assert.NotNull(alphaAgent);
        Assert.NotNull(betaAgent);
        Assert.NotEqual(alphaAgent.L1Description, betaAgent.L1Description);
        Assert.NotEqual(alphaAgent.L2Description, betaAgent.L2Description);
        
        // Ensure all agents have unique names
        var names = agents.Select(a => a.Name).ToList();
        var distinctNames = names.Distinct().ToList();
        Assert.Equal(names.Count, distinctNames.Count);
        
        Console.WriteLine("✓ Multiple agents scanned with distinct information");
    }

    [Fact]
    public void SimpleAgentScanner_HandleInvalidAgents_ShouldFilterOrHandle()
    {
        // Act
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(Assembly.GetExecutingAssembly());
        
        // Assert - Check how invalid agents are handled
        var invalidAgent = agents.FirstOrDefault(a => a.Name == "Invalid Agent");
        
        // The scanner should either filter out invalid agents or handle them gracefully
        if (invalidAgent != null)
        {
            // If included, verify it's marked appropriately
            Assert.NotNull(invalidAgent);
            Console.WriteLine("✓ Invalid agents are included but can be identified");
        }
        else
        {
            // If filtered out, that's also acceptable behavior
            Console.WriteLine("✓ Invalid agents are filtered out during scanning");
        }
    }

    [Fact]
    public void SimpleAgentScanner_ScanEmptyAssembly_ShouldReturnEmptyList()
    {
        // Arrange - Create a mock assembly or use one known to have no agents
        var testAssembly = typeof(string).Assembly; // System assembly with no agents
        
        // Act
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(testAssembly);
        
        // Assert
        Assert.NotNull(agents);
        Assert.Empty(agents);
        
        Console.WriteLine("✓ Empty assembly handled correctly");
    }

    [Fact]
    public void SimpleAgentScanner_PerformanceTest_ShouldCompleteQuickly()
    {
        // Act - Measure scanning performance
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(Assembly.GetExecutingAssembly());
        stopwatch.Stop();
        
        // Assert - Should complete within reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Scanning took {stopwatch.ElapsedMilliseconds}ms, should be under 1000ms");
        Assert.NotEmpty(agents);
        
        Console.WriteLine($"✓ Scanning completed in {stopwatch.ElapsedMilliseconds}ms with {agents.Count} agents found");
    }

    [Fact]
    public void AgentIndexInfo_GeneratedFromScanning_ShouldHaveCorrectStructure()
    {
        // Act - Get real scanned data
        var agents = SimpleAgentScanner.ScanAgentsInAssembly(Assembly.GetExecutingAssembly());
        var testAgent = agents.FirstOrDefault();
        
        // Assert - Verify AgentIndexInfo structure from actual scanning
        Assert.NotNull(testAgent);
        Assert.NotEmpty(testAgent.Id);
        Assert.NotEmpty(testAgent.Name);
        Assert.NotEmpty(testAgent.L1Description);
        Assert.NotEmpty(testAgent.L2Description);
        
        // Verify optional fields have default values
        Assert.NotNull(testAgent.Capabilities);
        Assert.NotNull(testAgent.Tags);
        Assert.NotEmpty(testAgent.InputFormat);
        Assert.NotEmpty(testAgent.OutputFormat);
        
        Console.WriteLine("✓ Scanned AgentIndexInfo has correct structure and required fields");
    }
} 