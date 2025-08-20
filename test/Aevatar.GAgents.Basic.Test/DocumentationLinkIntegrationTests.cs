using System;
using System.Linq;
using System.Reflection;
using Aevatar.GAgents.Basic.Common;
using Aevatar.GAgents.Twitter.Options;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Basic.Test;

public sealed class DocumentationLinkIntegrationTests : AevatarBasicTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DocumentationLinkIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    #region Twitter Options Integration Tests

    [Fact]
    public void InitTwitterOptionsDto_ShouldHaveDocumentationLinksOnExpectedProperties()
    {
        // Arrange
        var type = typeof(InitTwitterOptionsDto);
        var expectedPropertiesWithDocs = new[]
        {
            nameof(InitTwitterOptionsDto.ConsumerKey),
            nameof(InitTwitterOptionsDto.ConsumerSecret),
            nameof(InitTwitterOptionsDto.EncryptionPassword),
            nameof(InitTwitterOptionsDto.BearerToken)
        };

        // Act
        var propertiesWithDocumentation = type.GetProperties()
            .Where(p => p.GetCustomAttribute<DocumentationLinkAttribute>() != null)
            .ToArray();

        // Assert
        propertiesWithDocumentation.Length.ShouldBe(4);
        
        foreach (var expectedProperty in expectedPropertiesWithDocs)
        {
            propertiesWithDocumentation.Select(p => p.Name).ShouldContain(expectedProperty);
        }

        _testOutputHelper.WriteLine("InitTwitterOptionsDto properties with documentation links:");
        foreach (var prop in propertiesWithDocumentation)
        {
            var attr = prop.GetCustomAttribute<DocumentationLinkAttribute>();
            _testOutputHelper.WriteLine($"  - {prop.Name}: {attr?.DocumentationUrl}");
        }
    }

    [Fact]
    public void InitTwitterOptionsDto_AllDocumentationUrls_ShouldPointToTwitterDeveloper()
    {
        // Arrange
        var type = typeof(InitTwitterOptionsDto);
        const string expectedUrl = "https://developer.x.com";

        // Act
        var propertiesWithDocumentation = type.GetProperties()
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<DocumentationLinkAttribute>()
            })
            .Where(x => x.Attribute != null)
            .ToArray();

        // Assert
        propertiesWithDocumentation.All(x => x.Attribute!.DocumentationUrl == expectedUrl).ShouldBeTrue();
        
        _testOutputHelper.WriteLine($"All Twitter documentation URLs correctly point to: {expectedUrl}");
        foreach (var item in propertiesWithDocumentation)
        {
            _testOutputHelper.WriteLine($"  - {item.Property.Name}: ✓");
        }
    }

    [Theory]
    [InlineData(nameof(InitTwitterOptionsDto.ConsumerKey))]
    [InlineData(nameof(InitTwitterOptionsDto.ConsumerSecret))]
    [InlineData(nameof(InitTwitterOptionsDto.EncryptionPassword))]
    [InlineData(nameof(InitTwitterOptionsDto.BearerToken))]
    public void InitTwitterOptionsDto_SpecificProperty_ShouldHaveCorrectDocumentationLink(string propertyName)
    {
        // Arrange
        var type = typeof(InitTwitterOptionsDto);
        const string expectedUrl = "https://developer.x.com";

        // Act
        var property = type.GetProperty(propertyName);
        var attribute = property?.GetCustomAttribute<DocumentationLinkAttribute>();

        // Assert
        property.ShouldNotBeNull($"Property {propertyName} should exist");
        attribute.ShouldNotBeNull($"Property {propertyName} should have DocumentationLinkAttribute");
        attribute.DocumentationUrl.ShouldBe(expectedUrl);
        
        _testOutputHelper.WriteLine($"Property {propertyName} correctly has documentation URL: {attribute.DocumentationUrl}");
    }

    [Fact]
    public void InitTwitterOptionsDto_ReplyLimit_ShouldNotHaveDocumentationLink()
    {
        // Arrange
        var type = typeof(InitTwitterOptionsDto);

        // Act
        var property = type.GetProperty(nameof(InitTwitterOptionsDto.ReplyLimit));
        var attribute = property?.GetCustomAttribute<DocumentationLinkAttribute>();

        // Assert
        property.ShouldNotBeNull("ReplyLimit property should exist");
        attribute.ShouldBeNull("ReplyLimit property should not have DocumentationLinkAttribute");
        
        _testOutputHelper.WriteLine("ReplyLimit property correctly has no documentation link");
    }

    #endregion

    #region Metadata Extraction Tests

    [Fact]
    public void ExtractDocumentationMetadata_FromInitTwitterOptionsDto_ShouldProvideUsefulInformation()
    {
        // Arrange
        var type = typeof(InitTwitterOptionsDto);

        // Act
        var documentationMetadata = type.GetProperties()
            .Select(p => new
            {
                PropertyName = p.Name,
                PropertyType = p.PropertyType.Name,
                DocumentationUrl = p.GetCustomAttribute<DocumentationLinkAttribute>()?.DocumentationUrl,
                HasDocumentation = p.GetCustomAttribute<DocumentationLinkAttribute>() != null,
                DefaultValue = GetDefaultValue(p, type)
            })
            .OrderBy(x => x.PropertyName)
            .ToArray();

        // Assert
        documentationMetadata.Length.ShouldBe(7); // All properties (including inherited from ConfigurationBase)
        documentationMetadata.Count(x => x.HasDocumentation).ShouldBe(4); // Properties with documentation
        documentationMetadata.Count(x => !x.HasDocumentation).ShouldBe(3); // Properties without documentation

        _testOutputHelper.WriteLine("Complete metadata for InitTwitterOptionsDto:");
        _testOutputHelper.WriteLine("Property Name | Type | Has Docs | Documentation URL | Default Value");
        _testOutputHelper.WriteLine("-------------|------|----------|-------------------|-------------");
        
        foreach (var item in documentationMetadata)
        {
            var hasDocsIcon = item.HasDocumentation ? "✓" : "✗";
            var docUrl = item.DocumentationUrl ?? "N/A";
            var defaultVal = item.DefaultValue ?? "N/A";
            _testOutputHelper.WriteLine($"{item.PropertyName} | {item.PropertyType} | {hasDocsIcon} | {docUrl} | {defaultVal}");
        }
    }

    private static string? GetDefaultValue(PropertyInfo property, Type type)
    {
        try
        {
            var instance = Activator.CreateInstance(type);
            var value = property.GetValue(instance);
            return value?.ToString();
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Configuration Discovery Tests

    [Fact]
    public void DiscoverAllDocumentedConfigurations_InAssembly_ShouldFindTwitterConfig()
    {
        // Arrange
        var assemblies = new[]
        {
            typeof(DocumentationLinkAttribute).Assembly, // Basic assembly
            typeof(InitTwitterOptionsDto).Assembly // Twitter assembly
        };

        // Act
        var documentedTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetProperties()
                .Any(p => p.GetCustomAttribute<DocumentationLinkAttribute>() != null))
            .ToArray();

        // Assert
        documentedTypes.ShouldContain(typeof(InitTwitterOptionsDto));
        documentedTypes.Length.ShouldBeGreaterThan(0);

        _testOutputHelper.WriteLine($"Found {documentedTypes.Length} types with documented properties:");
        foreach (var type in documentedTypes)
        {
            var documentedPropsCount = type.GetProperties()
                .Count(p => p.GetCustomAttribute<DocumentationLinkAttribute>() != null);
            _testOutputHelper.WriteLine($"  - {type.Name}: {documentedPropsCount} documented properties");
        }
    }

    #endregion

    #region API Surface Tests

    [Fact]
    public void DocumentationLinkAttribute_PublicAPI_ShouldBeStable()
    {
        // Arrange
        var attributeType = typeof(DocumentationLinkAttribute);

        // Act
        var publicProperties = attributeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var publicConstructors = attributeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        publicProperties.Length.ShouldBe(2); // DocumentationUrl + TypeId (inherited from Attribute)
        
        var documentationUrlProperty = publicProperties.FirstOrDefault(p => p.Name == nameof(DocumentationLinkAttribute.DocumentationUrl));
        documentationUrlProperty.ShouldNotBeNull();
        documentationUrlProperty.PropertyType.ShouldBe(typeof(string));
        documentationUrlProperty.CanRead.ShouldBeTrue();
        documentationUrlProperty.CanWrite.ShouldBeFalse(); // Should be read-only

        publicConstructors.Length.ShouldBe(1);
        var constructor = publicConstructors[0];
        var parameters = constructor.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("documentationUrl");

        _testOutputHelper.WriteLine("Public API verification:");
        _testOutputHelper.WriteLine($"  - Properties: {publicProperties.Length}");
        _testOutputHelper.WriteLine($"  - DocumentationUrl: {publicProperties[0].PropertyType.Name} (ReadOnly: {!publicProperties[0].CanWrite})");
        _testOutputHelper.WriteLine($"  - Constructors: {publicConstructors.Length}");
        _testOutputHelper.WriteLine($"  - Constructor parameter: {parameters[0].ParameterType.Name} {parameters[0].Name}");
    }

    #endregion
}