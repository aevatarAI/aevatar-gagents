using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.Basic.BasicGEvent;
using Shouldly;
using System.Text.Json;

namespace Aevatar.GAgents.AIGAgent.Test;

/// <summary>
/// Unit tests for ConfigManagerGAgent
/// Tests configuration management functionality including updates, retrieval, and validation
/// </summary>
public sealed class ConfigManagerGAgentTests : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public ConfigManagerGAgentTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region Configuration Update Tests

    [Fact]
    public async Task ConfigManagerGAgent_UpdateConfig_ShouldSucceed()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var testConfig = new
        {
            DatabaseConnectionString = "Server=localhost;Database=TestDB;",
            ApiKey = "test-api-key-12345",
            MaxRetryAttempts = 3,
            TimeoutSeconds = 30
        };

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "DatabaseConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        // Act
        var response = await configManager.UpdateConfigAsync(updateEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ConfigType.ShouldBe("DatabaseConfig");
        response.ConfigJson.ShouldBe(updateEvent.ConfigJson);
        response.ErrorMessage.ShouldBeNull();

        // Verify state changes
        var state = await configManager.GetStateAsync();
        state.ConfigType.ShouldBe("DatabaseConfig");
        state.ConfigJson.ShouldBe(updateEvent.ConfigJson);
        state.TotalUpdates.ShouldBe(1);
        state.LastUpdated.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ConfigManagerGAgent_UpdateConfig_EmptyConfigType_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "", // Empty config type
            ConfigJson = "{\"key\": \"value\"}"
        };

        // Act
        var response = await configManager.UpdateConfigAsync(updateEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("ConfigType cannot be empty");
        response.ConfigJson.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ConfigManagerGAgent_UpdateConfig_EmptyConfigJson_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = "" // Empty JSON
        };

        // Act
        var response = await configManager.UpdateConfigAsync(updateEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("ConfigJson cannot be empty");
        response.ConfigJson.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ConfigManagerGAgent_UpdateConfig_InvalidJson_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = "{invalid json format" // Invalid JSON
        };

        // Act
        var response = await configManager.UpdateConfigAsync(updateEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("Invalid JSON format");
        response.ConfigJson.ShouldBe(string.Empty);
    }

    #endregion

    #region Configuration Request Tests

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_ShouldReturnStoredConfig()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var testConfig = new
        {
            ServiceUrl = "https://api.example.com",
            ApiVersion = "v2",
            EnableCaching = true
        };

        // First, update the configuration
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "ApiConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        await configManager.UpdateConfigAsync(updateEvent);

        // Act - Request the configuration
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "ApiConfig"
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ConfigType.ShouldBe("ApiConfig");
        response.ConfigJson.ShouldBe(updateEvent.ConfigJson);
        response.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_NoConfigStored_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "NonExistentConfig"
        };

        // Act
        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldBe("No configuration found");
        response.ConfigJson.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_TypeMismatch_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        // First, store a configuration
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "DatabaseConfig",
            ConfigJson = "{\"connectionString\": \"test\"}"
        };

        await configManager.UpdateConfigAsync(updateEvent);

        // Act - Request with different config type
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "ApiConfig" // Different type
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("Configuration type mismatch");
        response.ConfigJson.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_SpecificKey_ShouldReturnKeyValue()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var testConfig = new
        {
            DatabaseConnectionString = "Server=localhost;Database=TestDB;",
            ApiKey = "test-api-key-12345",
            MaxRetryAttempts = 3,
            TimeoutSeconds = 30
        };

        // Store configuration
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "AppConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        await configManager.UpdateConfigAsync(updateEvent);

        // Act - Request specific key
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "AppConfig",
            ConfigKey = "ApiKey"
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ConfigType.ShouldBe("AppConfig");

        // The value should be JSON serialized
        var extractedValue = JsonSerializer.Deserialize<JsonElement>(response.ConfigJson);
        extractedValue.ToString().ShouldBe("test-api-key-12345");
    }

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_NonExistentKey_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var testConfig = new { ExistingKey = "value" };

        // Store configuration
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        await configManager.UpdateConfigAsync(updateEvent);

        // Act - Request non-existent key
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "TestConfig",
            ConfigKey = "NonExistentKey"
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("Configuration key 'NonExistentKey' not found");
        response.ConfigJson.ShouldBe(string.Empty);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task ConfigManagerGAgent_MultipleUpdates_ShouldUpdateCounters()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var config1 = new { Setting1 = "value1" };
        var config2 = new { Setting2 = "value2" };

        // Act - Perform multiple updates
        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "Config1",
            ConfigJson = JsonSerializer.Serialize(config1)
        });

        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "Config1", // Same type, update
            ConfigJson = JsonSerializer.Serialize(config2)
        });

        // Assert
        var state = await configManager.GetStateAsync();
        state.TotalUpdates.ShouldBe(2);
        state.ConfigUpdateTimes.ShouldContainKey("Config1");
        state.ConfigUpdateTimes["Config1"].ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ConfigManagerGAgent_GetDescription_ShouldReturnCorrectDescription()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        // Act - Get description before any configuration
        var initialDescription = await configManager.GetDescriptionAsync();

        // Should contain "Not configured"
        initialDescription.ShouldContain("Not configured");

        // Update configuration
        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = "{\"test\": \"value\"}"
        });

        // Act - Get description after configuration
        var updatedDescription = await configManager.GetDescriptionAsync();

        // Assert
        updatedDescription.ShouldContain("TestConfig");
        updatedDescription.ShouldContain("Total updates: 1");
    }

    #endregion

    #region Complex Configuration Tests

    [Fact]
    public async Task ConfigManagerGAgent_ComplexJsonConfig_ShouldHandleCorrectly()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var complexConfig = new
        {
            Database = new
            {
                ConnectionString = "Server=localhost;Database=TestDB;",
                PoolSize = 10,
                EnableSSL = true
            },
            Api = new
            {
                BaseUrl = "https://api.example.com",
                Endpoints = new[]
                {
                    "/users",
                    "/orders",
                    "/products"
                },
                RateLimits = new
                {
                    RequestsPerMinute = 100,
                    BurstSize = 20
                }
            },
            Features = new
            {
                EnableCaching = true,
                EnableLogging = false,
                CacheExpiryMinutes = 30
            }
        };

        // Act
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "ComplexAppConfig",
            ConfigJson = JsonSerializer.Serialize(complexConfig)
        };

        var updateResponse = await configManager.UpdateConfigAsync(updateEvent);

        // Assert update succeeded
        updateResponse.Success.ShouldBeTrue();

        // Test retrieving nested values
        var databaseRequest = new ConfigRequestEvent
        {
            ConfigType = "ComplexAppConfig",
            ConfigKey = "Database"
        };

        var databaseResponse = await configManager.RequestConfigAsync(databaseRequest);
        databaseResponse.Success.ShouldBeTrue();

        var databaseConfig = JsonSerializer.Deserialize<JsonElement>(databaseResponse.ConfigJson);
        databaseConfig.GetProperty("ConnectionString").GetString().ShouldBe("Server=localhost;Database=TestDB;");
        databaseConfig.GetProperty("PoolSize").GetInt32().ShouldBe(10);
    }

    [Fact]
    public async Task ConfigManagerGAgent_ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());
        var tasks = new List<Task<ConfigResponseEvent>>();

        // Act - Execute concurrent update operations
        for (int i = 0; i < 5; i++)
        {
            var configIndex = i;
            var config = new { Index = configIndex, Value = $"config-{configIndex}" };

            var updateTask = configManager.UpdateConfigAsync(new ConfigUpdateEvent
            {
                ConfigType = "ConcurrentTestConfig",
                ConfigJson = JsonSerializer.Serialize(config)
            });

            tasks.Add(updateTask);
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All operations should succeed (last one wins)
        results.ShouldAllBe(r => r.Success);

        var finalState = await configManager.GetStateAsync();
        finalState.TotalUpdates.ShouldBe(5);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test configuration object for testing purposes
    /// </summary>
    private static object CreateTestConfiguration(string configName, string value)
    {
        return new
        {
            Name = configName,
            Value = value,
            CreatedAt = DateTime.UtcNow,
            IsEnabled = true,
            Settings = new
            {
                Timeout = 30,
                MaxRetries = 3
            }
        };
    }

    #endregion
}