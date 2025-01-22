
using System.Collections.Generic;

namespace Aevatar.GAgents.MicroAI.GAgent;

public static class LLMTypesConstant
{
    public const string AzureOpenAI = "azure_openai";
    public const string Bedrock = "bedrock";
    public const string GoogleGemini = "google_gemini";

    public static readonly List<string> AllSupportedLlmTypes = new List<string>
    {
        AzureOpenAI,
        Bedrock,
        GoogleGemini
    };
}