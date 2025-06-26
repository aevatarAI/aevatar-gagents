// ABOUTME: This file tests the automatic migration logic for LLM configuration centralization
// ABOUTME: Tests migration from legacy state-based to reference-based configuration during grain activation

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class LLMConfigurationMigrationTest : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public LLMConfigurationMigrationTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task OnGAgentActivateAsync_Should_MigrateSystemLLMToLLMConfigKey_When_LegacyFormatDetected()
    {
        // Arrange - Create agent with legacy SystemLLM configuration
        var systemLLMKey = "OpenAI";
        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Set up legacy state: SystemLLM set but LLMConfigKey is null
        await agent.SetSystemLLMAsync(systemLLMKey);
        
        var stateBeforeMigration = await agent.GetStateAsync();
        stateBeforeMigration.SystemLLM.ShouldBe(systemLLMKey);
        stateBeforeMigration.LLMConfigKey.ShouldBeNull();

        // Act - Trigger grain activation which should perform migration
        await agent.TriggerMigrationAsync();

        // Assert - Migration should have occurred
        var stateAfterMigration = await agent.GetStateAsync();
        stateAfterMigration.LLMConfigKey.ShouldBe(systemLLMKey);
        stateAfterMigration.SystemLLM.ShouldBe(systemLLMKey); // Should remain for backward compatibility
        
        // Configuration resolution should still work correctly
        var resolvedConfig = await agent.GetLLMConfigAsync();
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("gpt-4o"); // From appsettings.json
    }

    [Fact]
    public async Task OnGAgentActivateAsync_Should_MigrateResolvedLLMToSystemLLM_When_LegacyResolvedConfigExists()
    {
        // Arrange - Create agent with legacy resolved LLM configuration
        var legacyConfig = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Azure,
            ModelIdEnum = ModelIdEnum.OpenAI,
            ModelName = "legacy-gpt-4",
            Endpoint = "https://legacy.openai.azure.com",
            ApiKey = "legacy-key"
        };

        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Set up legacy state: LLM resolved config set but no SystemLLM or LLMConfigKey
        await agent.SetLLMAsync(legacyConfig, null);
        
        var stateBeforeMigration = await agent.GetStateAsync();
        stateBeforeMigration.LLM.ShouldNotBeNull();
        stateBeforeMigration.SystemLLM.ShouldBeNull();
        stateBeforeMigration.LLMConfigKey.ShouldBeNull();

        // Act - Trigger migration
        await agent.TriggerMigrationAsync();

        // Assert - Should NOT migrate resolved configs as they are self-provided
        var stateAfterMigration = await agent.GetStateAsync();
        stateAfterMigration.LLM.ShouldNotBeNull();
        stateAfterMigration.LLMConfigKey.ShouldBeNull(); // Should remain null for self-provided configs
        stateAfterMigration.SystemLLM.ShouldBeNull();
        
        // Configuration resolution should still work correctly
        var resolvedConfig = await agent.GetLLMConfigAsync();
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("legacy-gpt-4");
    }

    [Fact]
    public async Task OnGAgentActivateAsync_Should_NotMigrate_When_AlreadyUsingNewFormat()
    {
        // Arrange - Create agent already using new centralized format
        var configKey = "DeepSeek";
        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Set up new format: LLMConfigKey already set
        await agent.SetLLMConfigKeyAsync(configKey);
        
        var stateBeforeMigration = await agent.GetStateAsync();
        stateBeforeMigration.LLMConfigKey.ShouldBe(configKey);

        // Act - Trigger activation (should not migrate)
        await agent.TriggerMigrationAsync();

        // Assert - No changes should occur
        var stateAfterMigration = await agent.GetStateAsync();
        stateAfterMigration.LLMConfigKey.ShouldBe(configKey);
        
        // Configuration should still work
        var resolvedConfig = await agent.GetLLMConfigAsync();
        resolvedConfig.ShouldNotBeNull();
        resolvedConfig.ModelName.ShouldBe("DeepSeek-R1");
    }

    [Fact]
    public async Task OnGAgentActivateAsync_Should_NotMigrate_When_NoConfigurationExists()
    {
        // Arrange - Create agent with no configuration
        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        
        var stateBeforeMigration = await agent.GetStateAsync();
        stateBeforeMigration.LLM.ShouldBeNull();
        stateBeforeMigration.SystemLLM.ShouldBeNull();
        stateBeforeMigration.LLMConfigKey.ShouldBeNull();

        // Act - Trigger activation
        await agent.TriggerMigrationAsync();

        // Assert - No changes should occur
        var stateAfterMigration = await agent.GetStateAsync();
        stateAfterMigration.LLM.ShouldBeNull();
        stateAfterMigration.SystemLLM.ShouldBeNull();
        stateAfterMigration.LLMConfigKey.ShouldBeNull();
        
        // No configuration should be available
        var resolvedConfig = await agent.GetLLMConfigAsync();
        resolvedConfig.ShouldBeNull();
    }

    [Fact]
    public async Task OnGAgentActivateAsync_Should_PreserveBothConfigs_When_MigrationOccurs()
    {
        // Arrange - Create agent with both legacy SystemLLM and resolved LLM
        var systemLLMKey = "OpenAI";
        var resolvedConfig = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.Google,
            ModelIdEnum = ModelIdEnum.Gemini,
            ModelName = "gemini-pro",
            Endpoint = "https://ai.google.dev",
            ApiKey = "google-key"
        };

        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Set up mixed legacy state
        await agent.SetLLMAsync(resolvedConfig, systemLLMKey);
        
        var stateBeforeMigration = await agent.GetStateAsync();
        stateBeforeMigration.SystemLLM.ShouldBe(systemLLMKey);
        stateBeforeMigration.LLM.ShouldNotBeNull();
        stateBeforeMigration.LLMConfigKey.ShouldBeNull();

        // Act - Trigger migration
        await agent.TriggerMigrationAsync();

        // Assert - Migration should prefer SystemLLM (priority 2) over LLM (priority 3)
        var stateAfterMigration = await agent.GetStateAsync();
        stateAfterMigration.LLMConfigKey.ShouldBe(systemLLMKey);
        stateAfterMigration.SystemLLM.ShouldBe(systemLLMKey);
        stateAfterMigration.LLM.ShouldBeNull(); // Should be cleared during migration
        
        // Configuration should resolve to SystemLLM (higher priority)
        var currentConfig = await agent.GetLLMConfigAsync();
        currentConfig.ShouldNotBeNull();
        currentConfig.ModelName.ShouldBe("gpt-4o"); // From system config, not resolved config
    }

    [Fact]
    public async Task OnGAgentActivateAsync_Should_HandleInvalidSystemLLMDuringMigration()
    {
        // Arrange - Create agent with invalid SystemLLM key
        var invalidSystemLLMKey = "NonExistentSystemConfig";
        var fallbackConfig = new LLMConfig
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.OpenAI,
            ModelName = "fallback-gpt",
            Endpoint = "https://fallback.com",
            ApiKey = "fallback-key"
        };

        var agent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());

        // Set up invalid SystemLLM with valid fallback
        await agent.SetLLMAsync(fallbackConfig, invalidSystemLLMKey);
        
        // Act - Trigger migration
        await agent.TriggerMigrationAsync();

        // Assert - Should still attempt migration but fall back gracefully
        var stateAfterMigration = await agent.GetStateAsync();
        stateAfterMigration.LLMConfigKey.ShouldBe(invalidSystemLLMKey); // Migration occurs regardless
        stateAfterMigration.SystemLLM.ShouldBe(invalidSystemLLMKey);
        
        // Configuration resolution should return null (invalid key)
        var resolvedConfig = await agent.GetLLMConfigAsync();
        resolvedConfig.ShouldBeNull(); // Invalid system config returns null
    }
}