using System.ComponentModel;
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
    [Description("OpenAI's GPT models for text generation and completion")]
    OpenAI = 0,
    
    /// <summary>
    /// DeepSeek's advanced language models optimized for reasoning
    /// </summary>
    [Description("DeepSeek's advanced language models optimized for reasoning")]
    DeepSeek = 1,
    
    /// <summary>
    /// Azure-hosted OpenAI models with enterprise security
    /// </summary>
    [Description("Azure-hosted OpenAI models with enterprise security")]
    AzureOpenAI = 2,
    
    /// <summary>
    /// Azure OpenAI embedding models for semantic search
    /// </summary>
    [Description("Azure OpenAI embedding models for semantic search")]
    AzureOpenAIEmbeddings = 3,
    
    /// <summary>
    /// OpenAI embedding models for semantic understanding
    /// </summary>
    [Description("OpenAI embedding models for semantic understanding")]
    OpenAIEmbeddings = 4
}
