using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// ChatAI系统专用的LLM提供商枚举
/// 使用扩展方法提供结构化信息，支持自动序列化
/// </summary>
[GenerateSerializer]
public enum ChatAISystemLLMEnum
{
    /// <summary>
    /// OpenAI GPT models for text generation and completion
    /// </summary>
    [Description("OpenAI's GPT models for text generation and completion")]
    [LLMProviderMetadata(
        provider: "OpenAI",
        displayName: "OpenAI GPT",
        performance: "High-quality responses, fast inference",
        useCase: "General text generation, conversational AI",
        isEnterprise: false,
        isEmbedding: false,
        pricingTier: "Premium",
        features: "GPT-4,Function Calling,Vision,Streaming",
        icon: "openai",
        category: "TextGeneration")]
    OpenAI = 0,
    
    /// <summary>
    /// DeepSeek's advanced language models optimized for reasoning
    /// </summary>
    [Description("DeepSeek's advanced language models optimized for reasoning")]
    [LLMProviderMetadata(
        provider: "DeepSeek",
        displayName: "DeepSeek AI",
        performance: "Excellent reasoning capabilities, cost-effective",
        useCase: "Complex reasoning tasks, coding assistance",
        isEnterprise: false,
        isEmbedding: false,
        pricingTier: "Budget",
        features: "Code Generation,Reasoning,Math,Cost-Effective",
        icon: "deepseek",
        category: "TextGeneration")]
    DeepSeek = 1,
    
    /// <summary>
    /// Azure-hosted OpenAI models with enterprise security
    /// </summary>
    [Description("Azure-hosted OpenAI models with enterprise security")]
    [LLMProviderMetadata(
        provider: "Microsoft Azure",
        displayName: "Azure OpenAI",
        performance: "Enterprise-grade reliability, regional deployment",
        useCase: "Enterprise applications, regulated industries",
        isEnterprise: true,
        isEmbedding: false,
        pricingTier: "Enterprise",
        features: "Enterprise Security,SLA,Regional Deployment,Compliance",
        icon: "azure",
        category: "TextGeneration")]
    AzureOpenAI = 2,
    
    /// <summary>
    /// Azure OpenAI embedding models for semantic search
    /// </summary>
    [Description("Azure OpenAI embedding models for semantic search")]
    [LLMProviderMetadata(
        provider: "Microsoft Azure",
        displayName: "Azure OpenAI Embeddings",
        performance: "High-dimensional vector embeddings, batch processing",
        useCase: "Semantic search, recommendation systems, RAG",
        isEnterprise: true,
        isEmbedding: true,
        pricingTier: "Enterprise",
        features: "Batch Processing,Enterprise Security,High Throughput",
        icon: "azure",
        category: "Embeddings")]
    AzureOpenAIEmbeddings = 3,
    
    /// <summary>
    /// OpenAI embedding models for semantic understanding
    /// </summary>
    [Description("OpenAI embedding models for semantic understanding")]
    [LLMProviderMetadata(
        provider: "OpenAI",
        displayName: "OpenAI Embeddings",
        performance: "State-of-the-art text embeddings, real-time processing",
        useCase: "Document similarity, content recommendation, search",
        isEnterprise: false,
        isEmbedding: true,
        pricingTier: "Standard",
        features: "Real-time,High Quality,Semantic Search",
        icon: "openai",
        category: "Embeddings")]
    OpenAIEmbeddings = 4
}

/// <summary>
/// LLM提供商详细信息结构
/// </summary>
[GenerateSerializer]
public class LLMProviderInfo
{
    /// <summary>
    /// 提供商标识值
    /// </summary>
    [Id(0)] public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// 详细描述
    /// </summary>
    [Id(1)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 提供商名称
    /// </summary>
    [Id(2)] public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// 性能特点
    /// </summary>
    [Id(3)] public string Performance { get; set; } = string.Empty;
    
    /// <summary>
    /// 使用场景
    /// </summary>
    [Id(4)] public string UseCase { get; set; } = string.Empty;
    
    /// <summary>
    /// 转换为JSON字符串（用于Schema）
    /// </summary>
    /// <returns>JSON格式的字符串</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false 
        });
    }
    
    /// <summary>
    /// 从JSON字符串解析
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>LLMProviderInfo实例</returns>
    public static LLMProviderInfo? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<LLMProviderInfo>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// ChatAISystemLLMEnum扩展方法
/// </summary>
public static class ChatAISystemLLMEnumExtensions
{
    /// <summary>
    /// 获取LLM提供商的详细信息
    /// </summary>
    /// <param name="llmEnum">LLM枚举值</param>
    /// <returns>LLM提供商详细信息</returns>
    public static LLMProviderInfo GetProviderInfo(this ChatAISystemLLMEnum llmEnum)
    {
        return llmEnum switch
        {
            ChatAISystemLLMEnum.OpenAI => new LLMProviderInfo
            {
                Value = "OpenAI",
                Description = "OpenAI's GPT models for text generation and completion",
                Provider = "OpenAI",
                Performance = "High-quality responses, fast inference",
                UseCase = "General text generation, conversational AI"
            },
            ChatAISystemLLMEnum.DeepSeek => new LLMProviderInfo
            {
                Value = "DeepSeek",
                Description = "DeepSeek's advanced language models optimized for reasoning",
                Provider = "DeepSeek",
                Performance = "Excellent reasoning capabilities, cost-effective",
                UseCase = "Complex reasoning tasks, coding assistance"
            },
            ChatAISystemLLMEnum.AzureOpenAI => new LLMProviderInfo
            {
                Value = "AzureOpenAI",
                Description = "Azure-hosted OpenAI models with enterprise security",
                Provider = "Microsoft Azure",
                Performance = "Enterprise-grade reliability, regional deployment",
                UseCase = "Enterprise applications, regulated industries"
            },
            ChatAISystemLLMEnum.AzureOpenAIEmbeddings => new LLMProviderInfo
            {
                Value = "AzureOpenAIEmbeddings",
                Description = "Azure OpenAI embedding models for semantic search",
                Provider = "Microsoft Azure",
                Performance = "High-dimensional vector embeddings, batch processing",
                UseCase = "Semantic search, recommendation systems, RAG"
            },
            ChatAISystemLLMEnum.OpenAIEmbeddings => new LLMProviderInfo
            {
                Value = "OpenAIEmbeddings",
                Description = "OpenAI embedding models for semantic understanding",
                Provider = "OpenAI",
                Performance = "State-of-the-art text embeddings, real-time processing",
                UseCase = "Document similarity, content recommendation, search"
            },
            _ => new LLMProviderInfo
            {
                Value = llmEnum.ToString(),
                Description = "Unknown LLM provider",
                Provider = "Unknown",
                Performance = "Unknown",
                UseCase = "Unknown"
            }
        };
    }

    /// <summary>
    /// 获取LLM提供商信息的JSON表示（用于Schema）
    /// </summary>
    /// <param name="llmEnum">LLM枚举值</param>
    /// <returns>JSON字符串</returns>
    public static string GetProviderInfoJson(this ChatAISystemLLMEnum llmEnum)
    {
        return llmEnum.GetProviderInfo().ToJson();
    }

    /// <summary>
    /// 获取所有LLM提供商的信息
    /// </summary>
    /// <returns>所有LLM提供商信息的字典</returns>
    public static Dictionary<ChatAISystemLLMEnum, LLMProviderInfo> GetAllProviderInfos()
    {
        var result = new Dictionary<ChatAISystemLLMEnum, LLMProviderInfo>();
        
        foreach (ChatAISystemLLMEnum value in Enum.GetValues<ChatAISystemLLMEnum>())
        {
            result[value] = value.GetProviderInfo();
        }
        
        return result;
    }

    /// <summary>
    /// 获取所有LLM提供商信息的JSON数组（用于Schema扩展）
    /// </summary>
    /// <returns>JSON字符串数组</returns>
    public static string[] GetAllProviderInfoJsons()
    {
        return Enum.GetValues<ChatAISystemLLMEnum>()
            .Select(e => e.GetProviderInfoJson())
            .ToArray();
    }

    /// <summary>
    /// 获取Description特性值
    /// </summary>
    /// <param name="llmEnum">LLM枚举值</param>
    /// <returns>Description特性的值</returns>
    public static string GetDescription(this ChatAISystemLLMEnum llmEnum)
    {
        var field = llmEnum.GetType().GetField(llmEnum.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;
        
        return attribute?.Description ?? llmEnum.ToString();
    }
}
