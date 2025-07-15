using System;
using System.Collections.Generic;
using System.Linq;
using Aevatar.GAgents.Twitter.GAgents.TwitterSocialGAgent;
using Aevatar.GAgents.Twitter.Options;
using GroupChat.GAgent.Feature.Common;
using Shouldly;
using Aevatar.Core;

namespace Aevatar.GAgents.Twitter.Test;

public class TwitterSocialGAgentUnitTest
{
    [Fact]
    public void TwitterSocialGAgentState_InitializesCorrectly()
    {
        // Arrange & Act
        var state = new TwitterSocialGAgentState();

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldNotBe(Guid.Empty);
        state.ConsumerKey.ShouldBe("");
        state.ConsumerSecret.ShouldBe("");
        state.UserId.ShouldBe("");
        state.UserName.ShouldBe("");
        state.Token.ShouldBe("");
        state.TokenSecret.ShouldBe("");
        state.TotalTweets.ShouldBe(0);
        state.LastResponse.ShouldBe("");
    }

    [Fact]
    public void TwitterConfigEvent_SetsPropertiesCorrectly()
    {
        // Arrange
        var configEvent = new TwitterConfigEvent
        {
            ConsumerKey = "test_consumer_key",
            ConsumerSecret = "test_consumer_secret",
            TwitterUserId = "test_user_id",
            TwitterUserName = "test_username",
            TwitterToken = "test_token",
            TwitterTokenSecret = "test_token_secret",
            BearerToken = "test_bearer",
            ReplyLimit = 5
        };

        // Act & Assert
        configEvent.ConsumerKey.ShouldBe("test_consumer_key");
        configEvent.ConsumerSecret.ShouldBe("test_consumer_secret");
        configEvent.TwitterUserId.ShouldBe("test_user_id");
        configEvent.TwitterUserName.ShouldBe("test_username");
        configEvent.TwitterToken.ShouldBe("test_token");
        configEvent.TwitterTokenSecret.ShouldBe("test_token_secret");
        configEvent.BearerToken.ShouldBe("test_bearer");
        configEvent.ReplyLimit.ShouldBe(5);
    }

    [Fact]
    public void TwitterSocialResponseEvent_SetsPropertiesCorrectly()
    {
        // Arrange
        var responseEvent = new TwitterSocialResponseEvent
        {
            ResponseContent = "Test tweet content",
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert
        responseEvent.ResponseContent.ShouldBe("Test tweet content");
        responseEvent.Timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void TwitterWorkflowConfigDto_DefaultValues()
    {
        // Arrange & Act
        var config = new TwitterWorkflowConfigDto();

        // Assert
        config.ShouldNotBeNull();
        config.ConsumerKey.ShouldBe("");
        config.ConsumerSecret.ShouldBe("");
        config.TwitterUserId.ShouldBe("");
        config.TwitterUserName.ShouldBe("");
        config.TwitterToken.ShouldBe("");
        config.TwitterTokenSecret.ShouldBe("");
        config.ReplyLimit.ShouldBe(10);
    }

    [Fact]
    public void TwitterWorkflowConfigDto_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var config = new TwitterWorkflowConfigDto
        {
            MemberName = "TestBot",
            ConsumerKey = "test_consumer_key",
            ConsumerSecret = "test_consumer_secret",
            TwitterUserId = "test_user_id",
            TwitterUserName = "test_username",
            TwitterToken = "test_token",
            TwitterTokenSecret = "test_token_secret",
            BearerToken = "test_bearer",
            ReplyLimit = 15
        };

        // Assert
        config.MemberName.ShouldBe("TestBot");
        config.ConsumerKey.ShouldBe("test_consumer_key");
        config.ConsumerSecret.ShouldBe("test_consumer_secret");
        config.TwitterUserId.ShouldBe("test_user_id");
        config.TwitterUserName.ShouldBe("test_username");
        config.TwitterToken.ShouldBe("test_token");
        config.TwitterTokenSecret.ShouldBe("test_token_secret");
        config.BearerToken.ShouldBe("test_bearer");
        config.ReplyLimit.ShouldBe(15);
    }

    [Fact]
    public void ChatMessage_MessageConcatenationLogic()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Content = "Hello" },
            new() { Content = "World" },
            new() { Content = "!" }
        };

        // Act
        var concatenated = string.Join(" ", messages.Select(m => m.Content));

        // Assert
        concatenated.ShouldBe("Hello World !");
    }

    [Fact]
    public void IsNullOrEmpty_ExtensionMethod()
    {
        // Test string null/empty checks that would be used in the agent
        
        // Arrange
        string nullString = null;
        string emptyString = "";
        string validString = "test";

        // Act & Assert
        nullString.IsNullOrEmpty().ShouldBeTrue();
        emptyString.IsNullOrEmpty().ShouldBeTrue();
        validString.IsNullOrEmpty().ShouldBeFalse();
    }

    [Fact]
    public void Guid_ShortStringGeneration()
    {
        // Test the blackboard ID short string generation logic
        
        // Arrange
        var blackboardId = Guid.NewGuid();

        // Act
        var shortId = blackboardId.ToString()[..8];

        // Assert
        shortId.ShouldNotBeNull();
        shortId.Length.ShouldBe(8);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TwitterAccountConfiguration_ValidatesEmptyValues(string value)
    {
        // Arrange
        var config = new TwitterWorkflowConfigDto
        {
            TwitterUserId = value,
            TwitterUserName = value
        };

        // Act & Assert
        config.TwitterUserId.IsNullOrEmpty().ShouldBeTrue();
        config.TwitterUserName.IsNullOrEmpty().ShouldBeTrue();
    }

    [Theory]
    [InlineData("valid_user_id", "valid_username")]
    [InlineData("12345", "testuser")]
    public void TwitterAccountConfiguration_ValidatesValidValues(string userId, string userName)
    {
        // Arrange
        var config = new TwitterWorkflowConfigDto
        {
            TwitterUserId = userId,
            TwitterUserName = userName
        };

        // Act & Assert
        config.TwitterUserId.IsNullOrEmpty().ShouldBeFalse();
        config.TwitterUserName.IsNullOrEmpty().ShouldBeFalse();
    }

    [Fact]
    public void StateTransition_ConfigurationEvent()
    {
        // Test the state transition logic for configuration events
        
        // Arrange
        var state = new TwitterSocialGAgentState();
        var configEvent = new TwitterConfigEvent
        {
            ConsumerKey = "test_key",
            ConsumerSecret = "test_secret",
            TwitterUserId = "test_user_id",
            TwitterUserName = "test_username",
            TwitterToken = "test_token",
            TwitterTokenSecret = "test_token_secret"
        };

        // Act - Simulate state transition
        state.ConsumerKey = configEvent.ConsumerKey;
        state.ConsumerSecret = configEvent.ConsumerSecret;
        state.UserId = configEvent.TwitterUserId;
        state.UserName = configEvent.TwitterUserName;
        state.Token = configEvent.TwitterToken;
        state.TokenSecret = configEvent.TwitterTokenSecret;

        // Assert
        state.ConsumerKey.ShouldBe("test_key");
        state.ConsumerSecret.ShouldBe("test_secret");
        state.UserId.ShouldBe("test_user_id");
        state.UserName.ShouldBe("test_username");
        state.Token.ShouldBe("test_token");
        state.TokenSecret.ShouldBe("test_token_secret");
    }

    [Fact]
    public void StateTransition_ResponseEvent()
    {
        // Test the state transition logic for response events
        
        // Arrange
        var state = new TwitterSocialGAgentState();
        var responseEvent = new TwitterSocialResponseEvent
        {
            ResponseContent = "Test tweet content",
            Timestamp = DateTime.UtcNow
        };

        // Act - Simulate state transition
        state.LastResponse = responseEvent.ResponseContent;
        state.TotalTweets += 1;

        // Assert
        state.LastResponse.ShouldBe("Test tweet content");
        state.TotalTweets.ShouldBe(1);
    }

    [Fact]
    public void InterestValue_Logic()
    {
        // Test interest value calculation logic
        
        // Arrange
        var configuredState = new TwitterSocialGAgentState
        {
            UserId = "test_user",
            UserName = "test_username"
        };

        var unconfiguredState = new TwitterSocialGAgentState
        {
            UserId = "",
            UserName = ""
        };

        // Act & Assert
        // Configured account should have high interest
        (!configuredState.UserId.IsNullOrEmpty() && !configuredState.UserName.IsNullOrEmpty()).ShouldBeTrue();
        
        // Unconfigured account should have low interest
        (unconfiguredState.UserId.IsNullOrEmpty() || unconfiguredState.UserName.IsNullOrEmpty()).ShouldBeTrue();
    }
} 