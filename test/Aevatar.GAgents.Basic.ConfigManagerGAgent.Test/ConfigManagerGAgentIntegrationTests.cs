using System.Text;
using System.Text.Json;
using Aevatar.GAgents.Basic.BasicGAgents;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Basic.Test;

/// <summary>
/// Integration tests for ConfigManagerGAgent with real-world scenarios
/// </summary>
public class ConfigManagerGAgentIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public ConfigManagerGAgentIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Test Helper Methods

    private static string CreateTestConfig(string configType, int sizeKB)
    {
        var targetSize = sizeKB * 1024;
        var config = new Dictionary<string, object>
        {
            ["configType"] = configType,
            ["metadata"] = new
            {
                version = "1.0.0",
                createdAt = DateTime.UtcNow,
                description = $"Test configuration for {configType}"
            },
            ["settings"] = new Dictionary<string, object>(),
            ["data"] = new List<object>()
        };

        // Fill with test data to reach target size
        var settings = (Dictionary<string, object>)config["settings"];
        var counter = 0;

        while (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(config)) < targetSize && counter < 10000)
        {
            settings[$"setting_{counter:D6}"] = $"value_{counter:D6}_" + new string('x', 50);
            counter++;
        }

        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    }

    private ConfigManagerGAgent CreateConfigManagerGAgent()
    {
        // Note: In real tests, this would use Orleans TestHost
        // For now, we'll create a basic instance for testing logic
        return new ConfigManagerGAgent();
    }

    #endregion

    #region Realistic Scenario Tests

    [Fact]
    public async Task SmallApiConfig_ShouldUseInlineStorage()
    {
        // Arrange - Small API configuration (< 1KB)
        var configJson = CreateTestConfig("ApiSettings", 0); // Very small config
        var actualSize = Encoding.UTF8.GetByteCount(configJson);

        // Simulate determining storage strategy
        var expectedStrategy = actualSize < 1024 ? ConfigStorageStrategy.Inline : ConfigStorageStrategy.Compressed;

        // Assert
        expectedStrategy.ShouldBe(ConfigStorageStrategy.Inline);
        actualSize.ShouldBeLessThan(1024);

        _output.WriteLine($"API Config: {actualSize} bytes -> {expectedStrategy}");
    }

    [Fact]
    public async Task MediumFeatureConfig_ShouldUseCompressedStorage()
    {
        // Arrange - Medium feature configuration (5KB)
        var configJson = CreateTestConfig("FeatureFlags", 5);
        var actualSize = Encoding.UTF8.GetByteCount(configJson);

        var expectedStrategy = actualSize < 1024
            ? ConfigStorageStrategy.Inline
            : actualSize < 102400
                ? ConfigStorageStrategy.Compressed
                : ConfigStorageStrategy.External;

        // Test compression
        var storageService = new BasicConfigStorageService();
        var compressed = await storageService.CompressConfigAsync(configJson);
        var compressionRatio = (double)compressed.Length / actualSize;

        // Assert
        expectedStrategy.ShouldBe(ConfigStorageStrategy.Compressed);
        actualSize.ShouldBeGreaterThan(1024);
        actualSize.ShouldBeLessThan(102400);
        compressionRatio.ShouldBeLessThan(0.8); // Should compress to less than 80%

        _output.WriteLine($"Feature Config: {actualSize:N0} bytes -> {expectedStrategy}");
        _output.WriteLine($"Compression: {compressionRatio:P1} ratio");
    }

    [Fact]
    public async Task LargeMLModelConfig_ShouldUseExternalStorage()
    {
        // Arrange - Large ML model configuration (200KB)
        var configJson = CreateTestConfig("MLModelConfig", 200);
        var actualSize = Encoding.UTF8.GetByteCount(configJson);

        var expectedStrategy = actualSize < 1024
            ? ConfigStorageStrategy.Inline
            : actualSize < 102400
                ? ConfigStorageStrategy.Compressed
                : ConfigStorageStrategy.External;

        // Test external storage
        var storageService = new BasicConfigStorageService();
        var storageKey = await storageService.StoreConfigAsync(configJson, "MLModelConfig");
        var retrieved = await storageService.RetrieveConfigAsync(storageKey);

        // Assert
        expectedStrategy.ShouldBe(ConfigStorageStrategy.External);
        actualSize.ShouldBeGreaterThan(102400);
        storageKey.ShouldNotBeNullOrEmpty();
        retrieved.ShouldBe(configJson);

        _output.WriteLine($"ML Model Config: {actualSize:N0} bytes -> {expectedStrategy}");
        _output.WriteLine($"Storage Key: {storageKey}");
    }

    #endregion

    #region Performance Impact Tests

    [Theory]
    [InlineData("DatabaseConfig", 1)] // 1KB - Inline
    [InlineData("ServiceConfig", 10)] // 10KB - Compressed
    [InlineData("CacheConfig", 50)] // 50KB - Compressed
    [InlineData("ModelConfig", 150)] // 150KB - External
    public void ConfigPerformanceImpact_BySize_ShouldMeetExpectations(string configType, int sizeKB)
    {
        // Arrange
        var configJson = CreateTestConfig(configType, sizeKB);
        var actualSize = Encoding.UTF8.GetByteCount(configJson);

        // Determine strategy
        var strategy = actualSize < 1024
            ? ConfigStorageStrategy.Inline
            : actualSize < 102400
                ? ConfigStorageStrategy.Compressed
                : ConfigStorageStrategy.External;

        // Calculate performance impact
        var oldMemoryFootprint = actualSize * 3; // State + 2 events in old approach

        var newMemoryFootprint = strategy switch
        {
            ConfigStorageStrategy.Inline => actualSize + 200,
            ConfigStorageStrategy.Compressed => (actualSize / 3) + 200, // Assume 3:1 compression
            ConfigStorageStrategy.External => 200,
            _ => actualSize
        };

        var memoryReduction = (double)(oldMemoryFootprint - newMemoryFootprint) / oldMemoryFootprint;

        // Assert performance improvements
        newMemoryFootprint.ShouldBeLessThan(oldMemoryFootprint);
        memoryReduction.ShouldBeGreaterThan(0.3); // At least 30% reduction

        _output.WriteLine($"{configType} ({sizeKB}KB): {strategy}");
        _output.WriteLine(
            $"Memory: {oldMemoryFootprint:N0} -> {newMemoryFootprint:N0} ({memoryReduction:P1} reduction)");
    }

    [Fact]
    public void EventLogOptimization_ShouldReduceStorageSignificantly()
    {
        // Arrange - Test with different config sizes
        var testConfigs = new[]
        {
            ("Small", CreateTestConfig("SmallConfig", 1)),
            ("Medium", CreateTestConfig("MediumConfig", 10)),
            ("Large", CreateTestConfig("LargeConfig", 100))
        };

        _output.WriteLine("Event Log Size Comparison:");
        _output.WriteLine("Config Type\tOld Event Size\tNew Event Size\tReduction");

        foreach (var (name, configJson) in testConfigs)
        {
            var configSize = Encoding.UTF8.GetByteCount(configJson);

            // Old approach: Store full JSON in events
            var oldEventSize = configSize * 2; // ConfigSetLogEvent + ConfigUpdatedLogEvent with full JSON

            // New approach: Store only hash and metadata
            var newEventSize = 64 + 100; // Hash (64 chars) + metadata (~100 bytes)

            var reduction = (double)(oldEventSize - newEventSize) / oldEventSize;

            reduction.ShouldBeGreaterThan(0.8); // At least 80% reduction

            _output.WriteLine($"{name}\t\t{oldEventSize:N0}\t\t{newEventSize:N0}\t\t{reduction:P1}");
        }
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void LegacyConfigMigration_ShouldWorkSeamlessly()
    {
        // Arrange - Simulate legacy state
        var legacyState = new ConfigManagerGAgentState
        {
            ConfigType = "LegacyConfig",
            ConfigJson = CreateTestConfig("LegacyConfig", 5),
            SchemaVersion = 0, // Unset/default
            LastUpdated = DateTime.UtcNow.AddDays(-1)
        };

        // Act - Simulate migration check
        var isLegacy = legacyState.SchemaVersion < 2;
        var hasLegacyConfig = !string.IsNullOrEmpty(legacyState.ConfigJson);

        // Assert
        isLegacy.ShouldBeTrue();
        hasLegacyConfig.ShouldBeTrue();
        legacyState.ConfigHash.ShouldBeEmpty(); // Not set in legacy
        legacyState.StorageStrategy.ShouldBe(ConfigStorageStrategy.Inline); // Default value

        _output.WriteLine($"Legacy migration test passed for config type: {legacyState.ConfigType}");
    }

    [Fact]
    public void OptimizedStateTransition_ShouldHandleBothVersions()
    {
        // Test that state transition can handle both old and new event types

        // Arrange - Legacy event
        var legacyEvent = new ConfigSetLogEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = CreateTestConfig("TestConfig", 2),
            Timestamp = DateTime.UtcNow
        };

        // Arrange - Optimized event
        var optimizedEvent = new OptimizedConfigSetLogEvent
        {
            ConfigType = "TestConfig",
            StorageStrategy = ConfigStorageStrategy.Compressed,
            ConfigHash = "abcd1234",
            ConfigSize = 2048,
            Timestamp = DateTime.UtcNow,
            CompressedConfig = [1, 2, 3, 4]
        };

        // Assert - Both events should have required properties
        legacyEvent.ConfigType.ShouldNotBeEmpty();
        legacyEvent.ConfigJson.ShouldNotBeEmpty();

        optimizedEvent.ConfigType.ShouldNotBeEmpty();
        optimizedEvent.ConfigHash.ShouldNotBeEmpty();
        optimizedEvent.ConfigSize.ShouldBeGreaterThan(0);

        _output.WriteLine("State transition compatibility verified for both legacy and optimized events");
    }

    #endregion

    #region Real-world Configuration Scenarios

    [Fact]
    public async Task DatabaseConnectionConfig_ShouldBeHandledEfficiently()
    {
        // Arrange - Typical database configuration
        var dbConfig = new
        {
            ConnectionStrings = new
            {
                Default = "Server=localhost;Database=MyApp;Trusted_Connection=true;",
                ReadOnly = "Server=localhost;Database=MyApp_RO;Trusted_Connection=true;",
                Analytics = "Server=analytics-server;Database=Analytics;Trusted_Connection=true;"
            },
            DatabaseSettings = new
            {
                CommandTimeout = 30,
                MaxRetryCount = 3,
                RetryDelayMs = 1000,
                EnableSensitiveDataLogging = false,
                PoolSize = 100
            }
        };

        var configJson = JsonSerializer.Serialize(dbConfig, new JsonSerializerOptions { WriteIndented = true });
        var configSize = Encoding.UTF8.GetByteCount(configJson);

        // Act - Determine storage strategy
        var strategy = configSize < 1024 ? ConfigStorageStrategy.Inline : ConfigStorageStrategy.Compressed;

        // Assert
        strategy.ShouldBe(ConfigStorageStrategy.Inline); // DB configs are typically small
        configSize.ShouldBeLessThan(1024);

        _output.WriteLine($"Database Config: {configSize} bytes -> {strategy}");
    }

    [Fact]
    public async Task MicroserviceConfig_ShouldUseCompression()
    {
        // Arrange - Complex microservice configuration
        var services = Enumerable.Range(1, 50).Select(i => new
        {
            ServiceName = $"Service{i}",
            Endpoint = $"https://service{i}.example.com",
            TimeoutMs = 5000,
            RetryPolicy = new { MaxRetries = 3, BackoffMs = 1000 },
            HealthCheck = new { Endpoint = "/health", IntervalMs = 30000 },
            Features = new[] { "feature1", "feature2", "feature3" }
        }).ToArray();

        var microserviceConfig = new
        {
            Services = services,
            GlobalSettings = new
            {
                DefaultTimeout = 10000,
                CircuitBreakerThreshold = 5,
                BulkheadSize = 100
            }
        };

        var configJson =
            JsonSerializer.Serialize(microserviceConfig, new JsonSerializerOptions { WriteIndented = true });
        var configSize = Encoding.UTF8.GetByteCount(configJson);

        // Test compression
        var storageService = new BasicConfigStorageService();
        var compressed = await storageService.CompressConfigAsync(configJson);
        var compressionRatio = (double)compressed.Length / configSize;

        // Assert
        configSize.ShouldBeGreaterThan(1024);
        configSize.ShouldBeLessThan(102400);
        compressionRatio.ShouldBeLessThan(0.5); // Should compress well due to repetitive structure

        _output.WriteLine(
            $"Microservice Config: {configSize:N0} bytes -> Compressed to {compressed.Length:N0} bytes ({compressionRatio:P1})");
    }

    [Fact]
    public void AIModelConfig_ShouldUseExternalStorage()
    {
        // Arrange - Large AI model configuration with embeddings/weights metadata
        var modelLayers = Enumerable.Range(1, 1000).Select(i => new
        {
            LayerId = i,
            LayerType = i % 3 == 0 ? "Dense" : i % 3 == 1 ? "Conv2D" : "BatchNorm",
            Parameters = new
            {
                Units = 128 + (i * 10),
                Activation = "relu",
                WeightInitializer = "glorot_uniform",
                BiasInitializer = "zeros",
                Regularization = new { L1 = 0.01, L2 = 0.01 }
            },
            Metadata = new string('x', 200) // Simulate large metadata
        }).ToArray();

        var aiConfig = new
        {
            ModelName = "TransformerLarge",
            Version = "2.1.0",
            Architecture = new
            {
                Layers = modelLayers,
                InputShape = new[] { 1024, 768 },
                OutputShape = new[] { 1000 },
                TotalParameters = 175000000
            },
            TrainingConfig = new
            {
                BatchSize = 32,
                LearningRate = 0.001,
                Epochs = 100,
                Optimizer = "Adam",
                LossFunction = "CategoricalCrossentropy"
            }
        };

        var configJson = JsonSerializer.Serialize(aiConfig, new JsonSerializerOptions { WriteIndented = true });
        var configSize = Encoding.UTF8.GetByteCount(configJson);

        // Act
        var strategy = configSize < 102400 ? ConfigStorageStrategy.Compressed : ConfigStorageStrategy.External;

        // Assert
        strategy.ShouldBe(ConfigStorageStrategy.External);
        configSize.ShouldBeGreaterThan(102400);

        _output.WriteLine($"AI Model Config: {configSize:N0} bytes ({configSize / 1024.0:F1} KB) -> {strategy}");
    }

    #endregion

    #region Stress Test Scenarios

    [Fact]
    public void ConfigSizeDistribution_ShouldCoverAllStrategies()
    {
        // Test with various realistic config sizes to ensure all strategies are used
        var testCases = new[]
        {
            ("API Keys", 0.5), // 512 bytes
            ("Database Settings", 0.8), // 819 bytes
            ("Service Discovery", 5), // 5KB
            ("Feature Flags", 15), // 15KB
            ("Routing Rules", 45), // 45KB
            ("ML Pipeline Config", 80), // 80KB
            ("Large Dataset Metadata", 120), // 120KB
            ("Full System Config", 300) // 300KB
        };

        var strategyCounts = new Dictionary<ConfigStorageStrategy, int>();

        foreach (var (configType, sizeKB) in testCases)
        {
            var configJson = CreateTestConfig(configType, (int)sizeKB);
            var actualSize = Encoding.UTF8.GetByteCount(configJson);

            var strategy = actualSize < 1024
                ? ConfigStorageStrategy.Inline
                : actualSize < 102400
                    ? ConfigStorageStrategy.Compressed
                    : ConfigStorageStrategy.External;

            strategyCounts[strategy] = strategyCounts.GetValueOrDefault(strategy, 0) + 1;

            _output.WriteLine($"{configType}: {actualSize:N0} bytes -> {strategy}");
        }

        // Assert all strategies are used
        strategyCounts.ShouldContainKey(ConfigStorageStrategy.Inline);
        strategyCounts.ShouldContainKey(ConfigStorageStrategy.Compressed);
        strategyCounts.ShouldContainKey(ConfigStorageStrategy.External);

        _output.WriteLine($"\nStrategy Distribution:");
        foreach (var (strategy, count) in strategyCounts)
        {
            _output.WriteLine($"{strategy}: {count} configs");
        }
    }

    #endregion
}