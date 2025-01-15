namespace Aevatar.GAgents.MicroAI.GAgent;

/// <summary>
/// A class to store configuration options for all supported LLM.
/// This allows centralized management of all LLM-related settings.
/// </summary>
public class AIModelOptions
{
    /// <summary>
    /// Configuration options for Azure OpenAI.
    /// </summary>
    public MicroAIOptions AzureOpenAI { get; set; }
    
    /// <summary>
    /// Configuration options for Amazon Bedrock.
    /// </summary>
    public MicroAIOptions Bedrock { get; set; }
    
    /// <summary>
    /// Configuration options for Google Gemini.
    /// </summary>
    public MicroAIOptions GoogleGemini { get; set; }
}