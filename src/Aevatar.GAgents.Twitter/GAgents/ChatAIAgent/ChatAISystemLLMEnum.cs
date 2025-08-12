using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// ChatAI系统专用的LLM提供商枚举
/// </summary>
[GenerateSerializer]
public enum ChatAISystemLLMEnum
{
    /// <summary>
    /// OpenAI GPT models for text generation and completion
    /// </summary>
    OpenAI = 0,
    
    /// <summary>
    /// DeepSeek's advanced language models optimized for reasoning
    /// </summary>
    DeepSeek = 1,
    
    /// <summary>
    /// Azure-hosted OpenAI models with enterprise security
    /// </summary>
    AzureOpenAI = 2,
    
    /// <summary>
    /// Azure OpenAI embedding models for semantic search
    /// </summary>
    AzureOpenAIEmbeddings = 3,
    
    /// <summary>
    /// OpenAI embedding models for semantic understanding
    /// </summary>
    OpenAIEmbeddings = 4
}
