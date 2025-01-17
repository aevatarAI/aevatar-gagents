
namespace Aevatar.GAgents.MicroAI.GAgent;

/// <summary>
/// Configuration options for interacting with different Large Language Models (LLMs).
/// </summary>
public class MicroAIOptions
{
    /// <summary>
    /// Gets or sets the model identifier for the specific LLM (e.g., OpenAI, Bedrock, Gemini).
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional service identifier 
    /// (e.g., Bedrock service ID or a target model-specific identifier).
    /// </summary>
    public string? ServiceId { get; set; }

    /// <summary>
    /// Gets or sets the API key required to authenticate requests to the LLM service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the endpoint URL of the LLM service (e.g., Azure OpenAI endpoint).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
}