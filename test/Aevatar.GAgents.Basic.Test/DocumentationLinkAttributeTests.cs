using System;
using System.Linq;
using System.Reflection;
using Aevatar.GAgents.Basic.Common;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Basic.Test;

public sealed class DocumentationLinkAttributeTests : AevatarBasicTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DocumentationLinkAttributeTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidUrl_ShouldSetDocumentationUrl()
    {
        // Arrange
        const string expectedUrl = "https://docs.example.com/api";

        // Act
        var attribute = new DocumentationLinkAttribute(expectedUrl);

        // Assert
        attribute.DocumentationUrl.ShouldBe(expectedUrl);
        _testOutputHelper.WriteLine($"Successfully created attribute with URL: {attribute.DocumentationUrl}");
    }

    [Theory]
    [InlineData("https://developer.x.com")]
    [InlineData("https://platform.openai.com/docs")]
    [InlineData("https://docs.anthropic.com/claude")]
    [InlineData("https://www.postgresql.org/docs/")]
    public void Constructor_WithVariousValidUrls_ShouldSetDocumentationUrl(string url)
    {
        // Act
        var attribute = new DocumentationLinkAttribute(url);

        // Assert
        attribute.DocumentationUrl.ShouldBe(url);
        _testOutputHelper.WriteLine($"Valid URL test passed: {url}");
    }

    [Fact]
    public void Constructor_WithNullUrl_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new DocumentationLinkAttribute(null!));
        
        exception.Message.ShouldContain("Documentation URL cannot be null or empty");
        exception.ParamName.ShouldBe("documentationUrl");
        _testOutputHelper.WriteLine($"Null URL correctly threw exception: {exception.Message}");
    }

    [Fact]
    public void Constructor_WithEmptyUrl_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new DocumentationLinkAttribute(string.Empty));
        
        exception.Message.ShouldContain("Documentation URL cannot be null or empty");
        exception.ParamName.ShouldBe("documentationUrl");
        _testOutputHelper.WriteLine($"Empty URL correctly threw exception: {exception.Message}");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("  \t  \n  ")]
    public void Constructor_WithWhitespaceUrl_ShouldThrowArgumentException(string whitespaceUrl)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new DocumentationLinkAttribute(whitespaceUrl));
        
        exception.Message.ShouldContain("Documentation URL cannot be null or empty");
        exception.ParamName.ShouldBe("documentationUrl");
        _testOutputHelper.WriteLine($"Whitespace URL '{whitespaceUrl.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}' correctly threw exception");
    }

    #endregion

    #region Attribute Usage Tests

    [Fact]
    public void AttributeUsage_ShouldBeConfiguredCorrectly()
    {
        // Arrange
        var attributeType = typeof(DocumentationLinkAttribute);

        // Act
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Property | AttributeTargets.Field);
        attributeUsage.AllowMultiple.ShouldBeFalse();
        attributeUsage.Inherited.ShouldBeTrue();
        
        _testOutputHelper.WriteLine($"AttributeUsage correctly configured: ValidOn={attributeUsage.ValidOn}, AllowMultiple={attributeUsage.AllowMultiple}, Inherited={attributeUsage.Inherited}");
    }

    #endregion

    #region Property Application Tests

    private class TestConfigClass
    {
        [DocumentationLink("https://docs.example.com/api-key")]
        public string ApiKey { get; set; } = string.Empty;

        [DocumentationLink("https://docs.example.com/timeout")]
        public int TimeoutSeconds { get; set; }

        public string NonDocumentedProperty { get; set; } = string.Empty;
    }

    [Fact]
    public void AppliedToProperty_ShouldBeRetrievableViaReflection()
    {
        // Arrange
        var propertyInfo = typeof(TestConfigClass).GetProperty(nameof(TestConfigClass.ApiKey));

        // Act
        var attribute = propertyInfo?.GetCustomAttribute<DocumentationLinkAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.DocumentationUrl.ShouldBe("https://docs.example.com/api-key");
        _testOutputHelper.WriteLine($"Successfully retrieved attribute from property: {attribute.DocumentationUrl}");
    }

    [Fact]
    public void GetPropertiesWithDocumentationLink_ShouldReturnCorrectProperties()
    {
        // Arrange
        var type = typeof(TestConfigClass);

        // Act
        var propertiesWithDocumentation = type.GetProperties()
            .Where(p => p.GetCustomAttribute<DocumentationLinkAttribute>() != null)
            .ToArray();

        // Assert
        propertiesWithDocumentation.Length.ShouldBe(2);
        propertiesWithDocumentation.Select(p => p.Name).ShouldContain(nameof(TestConfigClass.ApiKey));
        propertiesWithDocumentation.Select(p => p.Name).ShouldContain(nameof(TestConfigClass.TimeoutSeconds));
        propertiesWithDocumentation.Select(p => p.Name).ShouldNotContain(nameof(TestConfigClass.NonDocumentedProperty));
        
        _testOutputHelper.WriteLine($"Found {propertiesWithDocumentation.Length} properties with documentation links");
        foreach (var prop in propertiesWithDocumentation)
        {
            var attr = prop.GetCustomAttribute<DocumentationLinkAttribute>();
            _testOutputHelper.WriteLine($"  - {prop.Name}: {attr?.DocumentationUrl}");
        }
    }

    #endregion

    #region Field Application Tests

    private class TestFieldClass
    {
        [DocumentationLink("https://docs.example.com/field")]
        public string DocumentedField = string.Empty;

        public string NonDocumentedField = string.Empty;
    }

    [Fact]
    public void AppliedToField_ShouldBeRetrievableViaReflection()
    {
        // Arrange
        var fieldInfo = typeof(TestFieldClass).GetField(nameof(TestFieldClass.DocumentedField));

        // Act
        var attribute = fieldInfo?.GetCustomAttribute<DocumentationLinkAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.DocumentationUrl.ShouldBe("https://docs.example.com/field");
        _testOutputHelper.WriteLine($"Successfully retrieved attribute from field: {attribute.DocumentationUrl}");
    }

    #endregion

    #region Real-world Usage Tests

    private class TwitterConfigExample
    {
        [DocumentationLink("https://developer.x.com")]
        public string ConsumerKey { get; set; } = "YOUR_TWITTER_API_KEY";

        [DocumentationLink("https://developer.x.com")]
        public string ConsumerSecret { get; set; } = "YOUR_API_SECRET";

        [DocumentationLink("https://developer.x.com")]
        public string BearerToken { get; set; } = "YOUR_BEARER_TOKEN";
    }

    [Fact]
    public void RealWorldUsage_TwitterConfig_ShouldWorkCorrectly()
    {
        // Arrange
        var type = typeof(TwitterConfigExample);

        // Act
        var documentsProperties = type.GetProperties()
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<DocumentationLinkAttribute>()
            })
            .Where(x => x.Attribute != null)
            .ToArray();

        // Assert
        documentsProperties.Length.ShouldBe(3);
        documentsProperties.All(x => x.Attribute!.DocumentationUrl == "https://developer.x.com").ShouldBeTrue();
        
        _testOutputHelper.WriteLine("Real-world Twitter config test results:");
        foreach (var item in documentsProperties)
        {
            _testOutputHelper.WriteLine($"  - {item.Property.Name}: {item.Attribute!.DocumentationUrl}");
        }
    }

    #endregion

    #region Documentation URL Validation Tests

    [Theory]
    [InlineData("https://docs.example.com")]
    [InlineData("http://localhost:8080/docs")]
    [InlineData("https://api.github.com/docs#authentication")]
    [InlineData("https://platform.openai.com/docs/api-reference/chat")]
    [InlineData("file://local-docs/api.html")]
    public void DocumentationUrl_VariousValidFormats_ShouldBeAccepted(string url)
    {
        // Act
        var attribute = new DocumentationLinkAttribute(url);

        // Assert
        attribute.DocumentationUrl.ShouldBe(url);
        _testOutputHelper.WriteLine($"Valid URL format accepted: {url}");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void DocumentationUrl_VeryLongUrl_ShouldBeAccepted()
    {
        // Arrange
        var longUrl = "https://docs.example.com/" + new string('a', 1000) + "/very-long-path";

        // Act
        var attribute = new DocumentationLinkAttribute(longUrl);

        // Assert
        attribute.DocumentationUrl.ShouldBe(longUrl);
        attribute.DocumentationUrl.Length.ShouldBeGreaterThan(1000);
        _testOutputHelper.WriteLine($"Very long URL accepted with length: {attribute.DocumentationUrl.Length}");
    }

    [Fact] 
    public void DocumentationUrl_SpecialCharacters_ShouldBeAccepted()
    {
        // Arrange
        var urlWithSpecialChars = "https://docs.example.com/api?param=value&other=测试#section";

        // Act
        var attribute = new DocumentationLinkAttribute(urlWithSpecialChars);

        // Assert
        attribute.DocumentationUrl.ShouldBe(urlWithSpecialChars);
        _testOutputHelper.WriteLine($"URL with special characters accepted: {urlWithSpecialChars}");
    }

    #endregion

    #region Inheritance Tests

    private class BaseConfigClass
    {
        [DocumentationLink("https://docs.example.com/base")]
        public virtual string BaseProperty { get; set; } = string.Empty;
    }

    private class DerivedConfigClass : BaseConfigClass
    {
        [DocumentationLink("https://docs.example.com/derived")]
        public override string BaseProperty { get; set; } = string.Empty;

        [DocumentationLink("https://docs.example.com/derived-only")]
        public string DerivedProperty { get; set; } = string.Empty;
    }

    [Fact]
    public void AttributeInheritance_ShouldWorkCorrectly()
    {
        // Arrange
        var baseType = typeof(BaseConfigClass);
        var derivedType = typeof(DerivedConfigClass);

        // Act
        var baseProperty = baseType.GetProperty(nameof(BaseConfigClass.BaseProperty));
        var derivedProperty = derivedType.GetProperty(nameof(DerivedConfigClass.BaseProperty));
        var derivedOnlyProperty = derivedType.GetProperty(nameof(DerivedConfigClass.DerivedProperty));

        var baseAttribute = baseProperty?.GetCustomAttribute<DocumentationLinkAttribute>();
        var derivedAttribute = derivedProperty?.GetCustomAttribute<DocumentationLinkAttribute>();
        var derivedOnlyAttribute = derivedOnlyProperty?.GetCustomAttribute<DocumentationLinkAttribute>();

        // Assert
        baseAttribute.ShouldNotBeNull();
        baseAttribute.DocumentationUrl.ShouldBe("https://docs.example.com/base");

        derivedAttribute.ShouldNotBeNull();
        derivedAttribute.DocumentationUrl.ShouldBe("https://docs.example.com/derived");

        derivedOnlyAttribute.ShouldNotBeNull();
        derivedOnlyAttribute.DocumentationUrl.ShouldBe("https://docs.example.com/derived-only");

        _testOutputHelper.WriteLine("Inheritance test results:");
        _testOutputHelper.WriteLine($"  - Base: {baseAttribute.DocumentationUrl}");
        _testOutputHelper.WriteLine($"  - Derived override: {derivedAttribute.DocumentationUrl}");
        _testOutputHelper.WriteLine($"  - Derived only: {derivedOnlyAttribute.DocumentationUrl}");
    }

    #endregion
}