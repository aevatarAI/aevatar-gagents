using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// LLM提供商元数据服务
/// 为Station提供额外的枚举元数据，无需修改枚举定义
/// </summary>
public static class LLMProviderMetadataService
{
    /// <summary>
    /// 获取ChatAISystemLLMEnum的扩展Schema信息
    /// Station可以调用此方法获取丰富的元数据
    /// </summary>
    /// <returns>扩展Schema对象</returns>
    public static object GetEnumExtendedSchema()
    {
        return new
        {
            enumType = nameof(ChatAISystemLLMEnum),
            enumDisplayName = "LLM Provider",
            enumDescription = "Select a Language Model provider for ChatAI system",
            
            // 基础枚举信息
            values = GetEnumBasicInfo(),
            
            // 扩展元数据
            metadata = GetEnumMetadata(),
            
            // UI配置
            uiConfig = GetUIConfiguration(),
            
            // 分组信息
            groups = GetProviderGroups()
        };
    }

    /// <summary>
    /// 获取基础枚举信息
    /// </summary>
    private static object[] GetEnumBasicInfo()
    {
        return new object[]
        {
            new { value = 0, name = "OpenAI", description = "OpenAI's GPT models for text generation and completion" },
            new { value = 1, name = "DeepSeek", description = "DeepSeek's advanced language models optimized for reasoning" },
            new { value = 2, name = "AzureOpenAI", description = "Azure-hosted OpenAI models with enterprise security" },
            new { value = 3, name = "AzureOpenAIEmbeddings", description = "Azure OpenAI embedding models for semantic search" },
            new { value = 4, name = "OpenAIEmbeddings", description = "OpenAI embedding models for semantic understanding" }
        };
    }

    /// <summary>
    /// 获取详细元数据
    /// </summary>
    private static Dictionary<string, object> GetEnumMetadata()
    {
        return new Dictionary<string, object>
        {
            ["OpenAI"] = new
            {
                provider = "OpenAI",
                displayName = "OpenAI GPT",
                category = "TextGeneration",
                performance = "High-quality responses, fast inference",
                useCase = "General text generation, conversational AI",
                features = new[] { "GPT-4", "Function Calling", "Vision", "Streaming" },
                pricing = new { tier = "Premium", model = "Pay-per-token" },
                isEnterprise = false,
                isEmbedding = false
            },
            ["DeepSeek"] = new
            {
                provider = "DeepSeek",
                displayName = "DeepSeek AI",
                category = "TextGeneration",
                performance = "Excellent reasoning capabilities, cost-effective",
                useCase = "Complex reasoning tasks, coding assistance",
                features = new[] { "Code Generation", "Reasoning", "Math", "Cost-Effective" },
                pricing = new { tier = "Budget", model = "Pay-per-token" },
                isEnterprise = false,
                isEmbedding = false
            },
            ["AzureOpenAI"] = new
            {
                provider = "Microsoft Azure",
                displayName = "Azure OpenAI",
                category = "TextGeneration",
                performance = "Enterprise-grade reliability, regional deployment",
                useCase = "Enterprise applications, regulated industries",
                features = new[] { "Enterprise Security", "SLA", "Regional Deployment", "Compliance" },
                pricing = new { tier = "Enterprise", model = "Subscription + Pay-per-token" },
                isEnterprise = true,
                isEmbedding = false
            },
            ["AzureOpenAIEmbeddings"] = new
            {
                provider = "Microsoft Azure",
                displayName = "Azure OpenAI Embeddings",
                category = "Embeddings",
                performance = "High-dimensional vector embeddings, batch processing",
                useCase = "Semantic search, recommendation systems, RAG",
                features = new[] { "Batch Processing", "Enterprise Security", "High Throughput" },
                pricing = new { tier = "Enterprise", model = "Subscription + Pay-per-token" },
                isEnterprise = true,
                isEmbedding = true
            },
            ["OpenAIEmbeddings"] = new
            {
                provider = "OpenAI",
                displayName = "OpenAI Embeddings",
                category = "Embeddings",
                performance = "State-of-the-art text embeddings, real-time processing",
                useCase = "Document similarity, content recommendation, search",
                features = new[] { "Real-time", "High Quality", "Semantic Search" },
                pricing = new { tier = "Standard", model = "Pay-per-token" },
                isEnterprise = false,
                isEmbedding = true
            }
        };
    }

    /// <summary>
    /// 获取UI配置
    /// </summary>
    private static object GetUIConfiguration()
    {
        return new
        {
            displayFormat = "grouped-dropdown",
            showDescriptions = true,
            showProviderInfo = true,
            showFeatures = true,
            enableSearch = true,
            defaultValue = "OpenAI",
            placeholder = "Select LLM Provider..."
        };
    }

    /// <summary>
    /// 获取分组信息
    /// </summary>
    private static object GetProviderGroups()
    {
        return new
        {
            byProvider = new Dictionary<string, object>
            {
                ["OpenAI"] = new
                {
                    displayName = "OpenAI",
                    icon = "openai",
                    items = new[] { "OpenAI", "OpenAIEmbeddings" }
                },
                ["Microsoft Azure"] = new
                {
                    displayName = "Microsoft Azure",
                    icon = "azure",
                    items = new[] { "AzureOpenAI", "AzureOpenAIEmbeddings" }
                },
                ["DeepSeek"] = new
                {
                    displayName = "DeepSeek",
                    icon = "deepseek",
                    items = new[] { "DeepSeek" }
                }
            },
            byCategory = new Dictionary<string, object>
            {
                ["TextGeneration"] = new
                {
                    displayName = "Text Generation",
                    description = "Models for generating text, conversations, and content",
                    items = new[] { "OpenAI", "DeepSeek", "AzureOpenAI" }
                },
                ["Embeddings"] = new
                {
                    displayName = "Text Embeddings",
                    description = "Models for converting text into vector representations",
                    items = new[] { "OpenAIEmbeddings", "AzureOpenAIEmbeddings" }
                }
            }
        };
    }

    /// <summary>
    /// 获取特定枚举值的元数据
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>元数据对象</returns>
    public static object? GetMetadataForValue(ChatAISystemLLMEnum enumValue)
    {
        var metadata = GetEnumMetadata();
        var enumName = enumValue.ToString();
        
        return metadata.TryGetValue(enumName, out var value) ? value : null;
    }

    /// <summary>
    /// 获取JSON格式的扩展Schema
    /// </summary>
    /// <returns>JSON字符串</returns>
    public static string GetEnumExtendedSchemaJson()
    {
        var schema = GetEnumExtendedSchema();
        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    /// <summary>
    /// 检查枚举值是否为企业级
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>是否为企业级</returns>
    public static bool IsEnterpriseProvider(ChatAISystemLLMEnum enumValue)
    {
        return enumValue == ChatAISystemLLMEnum.AzureOpenAI || 
               enumValue == ChatAISystemLLMEnum.AzureOpenAIEmbeddings;
    }

    /// <summary>
    /// 检查枚举值是否为嵌入模型
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>是否为嵌入模型</returns>
    public static bool IsEmbeddingProvider(ChatAISystemLLMEnum enumValue)
    {
        return enumValue == ChatAISystemLLMEnum.OpenAIEmbeddings || 
               enumValue == ChatAISystemLLMEnum.AzureOpenAIEmbeddings;
    }

    /// <summary>
    /// 获取推荐的提供商（基于使用场景）
    /// </summary>
    /// <param name="useCase">使用场景</param>
    /// <returns>推荐的枚举值列表</returns>
    public static ChatAISystemLLMEnum[] GetRecommendedProviders(string useCase)
    {
        return useCase.ToLowerInvariant() switch
        {
            "enterprise" => new[] { ChatAISystemLLMEnum.AzureOpenAI },
            "embeddings" => new[] { ChatAISystemLLMEnum.OpenAIEmbeddings, ChatAISystemLLMEnum.AzureOpenAIEmbeddings },
            "coding" => new[] { ChatAISystemLLMEnum.DeepSeek, ChatAISystemLLMEnum.OpenAI },
            "budget" => new[] { ChatAISystemLLMEnum.DeepSeek },
            _ => new[] { ChatAISystemLLMEnum.OpenAI, ChatAISystemLLMEnum.DeepSeek }
        };
    }
}