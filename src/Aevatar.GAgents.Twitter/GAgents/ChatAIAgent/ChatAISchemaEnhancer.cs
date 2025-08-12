using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// ChatAI Schema增强器，为NJsonSchema生成提供额外的元数据支持
/// </summary>
public static class ChatAISchemaEnhancer
{
    /// <summary>
    /// 为ChatAISystemLLMEnum生成增强的Schema定义
    /// </summary>
    /// <returns>增强的Schema对象</returns>
    public static object GetEnhancedEnumSchema()
    {
        var enumValues = Enum.GetValues<ChatAISystemLLMEnum>().Cast<int>().ToArray();
        var enumNames = Enum.GetNames<ChatAISystemLLMEnum>();
        var enumDescriptions = GetEnumDescriptions();
        var enumMetadata = GetEnumMetadata();

        return new
        {
            type = "integer",
            @enum = enumValues,
            description = "ChatAI系统专用的LLM提供商枚举，支持多种AI服务提供商",
            title = "LLM Provider",
            
            // 标准扩展
            xEnumNames = enumNames,
            xEnumDescriptions = enumDescriptions,
            
            // 自定义扩展 - 结构化元数据
            xEnumMetadata = enumMetadata,
            xProviderInfo = GetProviderInfoMapping(),
            
            // UI 相关扩展
            xDisplayFormat = "dropdown",
            xShowDetails = true,
            xGroupByProvider = GetProviderGroups()
        };
    }

    /// <summary>
    /// 获取枚举值的Description特性内容
    /// </summary>
    /// <returns>Description数组</returns>
    private static string[] GetEnumDescriptions()
    {
        return Enum.GetValues<ChatAISystemLLMEnum>()
            .Select(enumValue =>
            {
                var field = enumValue.GetType().GetField(enumValue.ToString());
                var descriptionAttr = field?.GetCustomAttribute<DescriptionAttribute>();
                return descriptionAttr?.Description ?? enumValue.ToString();
            })
            .ToArray();
    }

    /// <summary>
    /// 获取结构化的枚举元数据
    /// </summary>
    /// <returns>元数据对象数组</returns>
    private static object[] GetEnumMetadata()
    {
        return Enum.GetValues<ChatAISystemLLMEnum>()
            .Select(enumValue =>
            {
                var providerInfo = enumValue.GetProviderInfo();
                return new
                {
                    value = (int)enumValue,
                    name = enumValue.ToString(),
                    provider = providerInfo.Provider,
                    description = providerInfo.Description,
                    performance = providerInfo.Performance,
                    useCase = providerInfo.UseCase,
                    category = GetProviderCategory(enumValue),
                    isEmbedding = enumValue.ToString().Contains("Embedding"),
                    isEnterprise = enumValue.ToString().Contains("Azure")
                };
            })
            .ToArray();
    }

    /// <summary>
    /// 获取提供商信息映射
    /// </summary>
    /// <returns>提供商信息字典</returns>
    private static Dictionary<string, object> GetProviderInfoMapping()
    {
        var result = new Dictionary<string, object>();
        
        foreach (ChatAISystemLLMEnum enumValue in Enum.GetValues<ChatAISystemLLMEnum>())
        {
            var providerInfo = enumValue.GetProviderInfo();
            result[enumValue.ToString()] = new
            {
                displayName = GetDisplayName(enumValue),
                category = GetProviderCategory(enumValue),
                metadata = providerInfo,
                features = GetProviderFeatures(enumValue),
                pricing = GetProviderPricing(enumValue)
            };
        }
        
        return result;
    }

    /// <summary>
    /// 获取提供商分组信息
    /// </summary>
    /// <returns>分组信息</returns>
    private static object GetProviderGroups()
    {
        return new
        {
            byProvider = new Dictionary<string, string[]>
            {
                ["OpenAI"] = new[] { "OpenAI", "OpenAIEmbeddings" },
                ["Microsoft Azure"] = new[] { "AzureOpenAI", "AzureOpenAIEmbeddings" },
                ["DeepSeek"] = new[] { "DeepSeek" }
            },
            byCategory = new Dictionary<string, string[]>
            {
                ["Text Generation"] = new[] { "OpenAI", "DeepSeek", "AzureOpenAI" },
                ["Embeddings"] = new[] { "OpenAIEmbeddings", "AzureOpenAIEmbeddings" },
                ["Enterprise"] = new[] { "AzureOpenAI", "AzureOpenAIEmbeddings" }
            }
        };
    }

    /// <summary>
    /// 获取提供商类别
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>类别字符串</returns>
    private static string GetProviderCategory(ChatAISystemLLMEnum enumValue)
    {
        return enumValue switch
        {
            ChatAISystemLLMEnum.OpenAI or ChatAISystemLLMEnum.DeepSeek or ChatAISystemLLMEnum.AzureOpenAI => "TextGeneration",
            ChatAISystemLLMEnum.OpenAIEmbeddings or ChatAISystemLLMEnum.AzureOpenAIEmbeddings => "Embeddings",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// 获取显示名称
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>显示名称</returns>
    private static string GetDisplayName(ChatAISystemLLMEnum enumValue)
    {
        return enumValue switch
        {
            ChatAISystemLLMEnum.OpenAI => "OpenAI GPT",
            ChatAISystemLLMEnum.DeepSeek => "DeepSeek AI",
            ChatAISystemLLMEnum.AzureOpenAI => "Azure OpenAI",
            ChatAISystemLLMEnum.AzureOpenAIEmbeddings => "Azure OpenAI Embeddings",
            ChatAISystemLLMEnum.OpenAIEmbeddings => "OpenAI Embeddings",
            _ => enumValue.ToString()
        };
    }

    /// <summary>
    /// 获取提供商特性
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>特性列表</returns>
    private static string[] GetProviderFeatures(ChatAISystemLLMEnum enumValue)
    {
        return enumValue switch
        {
            ChatAISystemLLMEnum.OpenAI => new[] { "GPT-4", "Function Calling", "Vision", "Streaming" },
            ChatAISystemLLMEnum.DeepSeek => new[] { "Code Generation", "Reasoning", "Math", "Cost-Effective" },
            ChatAISystemLLMEnum.AzureOpenAI => new[] { "Enterprise Security", "SLA", "Regional Deployment", "Compliance" },
            ChatAISystemLLMEnum.AzureOpenAIEmbeddings => new[] { "Batch Processing", "Enterprise Security", "High Throughput" },
            ChatAISystemLLMEnum.OpenAIEmbeddings => new[] { "Real-time", "High Quality", "Semantic Search" },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// 获取定价信息
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>定价信息</returns>
    private static object GetProviderPricing(ChatAISystemLLMEnum enumValue)
    {
        return enumValue switch
        {
            ChatAISystemLLMEnum.OpenAI => new { tier = "Premium", model = "Pay-per-token" },
            ChatAISystemLLMEnum.DeepSeek => new { tier = "Budget", model = "Pay-per-token" },
            ChatAISystemLLMEnum.AzureOpenAI => new { tier = "Enterprise", model = "Subscription + Pay-per-token" },
            ChatAISystemLLMEnum.AzureOpenAIEmbeddings => new { tier = "Enterprise", model = "Subscription + Pay-per-token" },
            ChatAISystemLLMEnum.OpenAIEmbeddings => new { tier = "Standard", model = "Pay-per-token" },
            _ => new { tier = "Unknown", model = "Unknown" }
        };
    }

    /// <summary>
    /// 生成完整的Schema JSON字符串
    /// </summary>
    /// <returns>JSON字符串</returns>
    public static string GetEnhancedSchemaJson()
    {
        var schema = GetEnhancedEnumSchema();
        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
}