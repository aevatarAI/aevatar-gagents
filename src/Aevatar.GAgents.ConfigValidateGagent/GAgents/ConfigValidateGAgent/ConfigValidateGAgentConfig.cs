using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.ConfigValidateGagent.GAgents.ConfigValidateGAgent;

[GenerateSerializer]
[Description("Simple two-parameter HTTP input type validation demonstration using InputType and InputContent")]
public class ConfigValidateGAgentConfig : ConfigurationBase, IValidatableObject
{
    [Id(0)]
    [Description("Specifies the HTTP request input type")]
    public InputType InputType { get; set; } = InputType.JSON;

    [Id(1)]
    [Description("HTTP request input content")]
    public string InputContent { get; set; } = string.Empty;

    /// <summary>
    /// Implements custom validation logic to demonstrate HTTP input type validation
    /// </summary>
    /// <param name="validationContext">Validation context</param>
    /// <returns>List of validation errors</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();

        // HTTP input type validation - validate InputContent based on specified InputType
        var inputValidationErrors = ValidateInputByType(InputContent, InputType);
        errors.AddRange(inputValidationErrors);

        return errors;
    }

    /// <summary>
    /// Validates input content based on the specified HTTP input type
    /// </summary>
    /// <param name="input">Input content to validate</param>
    /// <param name="inputType">HTTP input type</param>
    /// <returns>List of validation errors</returns>
    private IEnumerable<ValidationResult> ValidateInputByType(string input, InputType inputType)
    {
        var errors = new List<ValidationResult>();

        try
        {
            switch (inputType)
            {
                case InputType.None:
                    // None type should not have input content
                    if (!string.IsNullOrEmpty(input))
                    {
                        errors.Add(new ValidationResult(
                            $"When 'None' type is selected, there should be no input content, but provided: '{input}'",
                            new[] { nameof(InputContent) }));
                    }
                    break;

                case InputType.FormData:
                    // FormData should contain key-value pair formatted content
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        errors.Add(new ValidationResult(
                            "FormData type requires form data content. Example: key1=value1&key2=value2",
                            new[] { nameof(InputContent) }));
                    }
                    else if (!input.Contains("="))
                    {
                        errors.Add(new ValidationResult(
                            $"FormData format should contain key-value pairs. Current content: '{input}', Example: key1=value1&key2=value2",
                            new[] { nameof(InputContent) }));
                    }
                    break;

                case InputType.XWwwFormUrlencoded:
                    // URL-encoded form data validation
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        errors.Add(new ValidationResult(
                            "URL-encoded form type requires encoded data. Example: key1=value1&key2=value2",
                            new[] { nameof(InputContent) }));
                    }
                    else if (!input.Contains("=") || (!input.Contains("&") && input.Split('=').Length > 2))
                    {
                        errors.Add(new ValidationResult(
                            $"URL-encoded form format error. Current content: '{input}', Example: key1=value1&key2=value2",
                            new[] { nameof(InputContent) }));
                    }
                    break;

                case InputType.JSON:
                    // JSON format validation
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        errors.Add(new ValidationResult(
                            "JSON type requires JSON formatted data. Example: {\"key\":\"value\"}",
                            new[] { nameof(InputContent) }));
                    }
                    else
                    {
                        try
                        {
                            System.Text.Json.JsonDocument.Parse(input);
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            errors.Add(new ValidationResult(
                                $"Input content is not valid JSON format. Current content: '{input}', Example: {{\"key\":\"value\"}}",
                                new[] { nameof(InputContent) }));
                        }
                    }
                    break;

                case InputType.Raw:
                    // Raw type can be any text content, only requires non-empty check
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        errors.Add(new ValidationResult(
                            "Raw type requires raw text content",
                            new[] { nameof(InputContent) }));
                    }
                    break;

                case InputType.Binary:
                    // Binary type checks if it looks like binary data representation
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        errors.Add(new ValidationResult(
                            "Binary type requires binary data content",
                            new[] { nameof(InputContent) }));
                    }
                    else if (input.Length < 10)
                    {
                        errors.Add(new ValidationResult(
                            $"Binary data content seems too short. Current length: {input.Length} characters",
                            new[] { nameof(InputContent) }));
                    }
                    break;

                default:
                    errors.Add(new ValidationResult(
                        $"Unsupported input type: {inputType}",
                        new[] { nameof(InputType) }));
                    break;
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationResult(
                $"Error occurred during validation: {ex.Message}",
                new[] { nameof(InputContent) }));
        }

        return errors;
    }
}

/// <summary>
/// HTTP request input type enumeration - based on user-provided standard types
/// </summary>
[GenerateSerializer]
public enum InputType
{
    [Description("No request body")]
    None = 0,

    [Description("Form data (multipart/form-data)")]
    FormData = 1,

    [Description("URL-encoded form (application/x-www-form-urlencoded)")]
    XWwwFormUrlencoded = 2,

    [Description("JSON data (application/json)")]
    JSON = 3,

    [Description("Raw text data")]
    Raw = 4,

    [Description("Binary data")]
    Binary = 5
}