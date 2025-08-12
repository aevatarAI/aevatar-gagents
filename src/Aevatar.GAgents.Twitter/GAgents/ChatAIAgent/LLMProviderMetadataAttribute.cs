using System;
using System.Linq;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// LLM提供商元数据信息
/// </summary>
public class LLMProviderMetadata
{
    /// <summary>
    /// 提供商标识值
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// 详细描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 提供商名称
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 性能特点
    /// </summary>
    public string Performance { get; set; } = string.Empty;
    
    /// <summary>
    /// 使用场景
    /// </summary>
    public string UseCase { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否为企业级
    /// </summary>
    public bool IsEnterprise { get; set; }
    
    /// <summary>
    /// 是否为嵌入模型
    /// </summary>
    public bool IsEmbedding { get; set; }
    
    /// <summary>
    /// 定价层级
    /// </summary>
    public string PricingTier { get; set; } = string.Empty;
    
    /// <summary>
    /// 特性列表
    /// </summary>
    public string[] Features { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// 图标名称
    /// </summary>
    public string Icon { get; set; } = string.Empty;
    
    /// <summary>
    /// 分类
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// LLM提供商元数据特性
/// 为枚举值提供结构化的元数据，可被NJsonSchema处理
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class LLMProviderMetadataAttribute : EnumMetadataAttributeBase<LLMProviderMetadata>
{
    private readonly string _provider;
    private readonly string _displayName;
    private readonly string _performance;
    private readonly string _useCase;
    private readonly bool _isEnterprise;
    private readonly bool _isEmbedding;
    private readonly string _pricingTier;
    private readonly string _features;
    private readonly string _icon;
    private readonly string _category;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LLMProviderMetadataAttribute(
        string provider,
        string displayName,
        string performance,
        string useCase,
        bool isEnterprise = false,
        bool isEmbedding = false,
        string pricingTier = "Standard",
        string features = "",
        string icon = "",
        string category = "TextGeneration")
    {
        _provider = provider;
        _displayName = displayName;
        _performance = performance;
        _useCase = useCase;
        _isEnterprise = isEnterprise;
        _isEmbedding = isEmbedding;
        _pricingTier = pricingTier;
        _features = features;
        _icon = icon;
        _category = category;
        
        // 设置基类属性
        Category = "LLMProvider";
        Identifier = provider;
        DisplayName = displayName;
        Description = performance;
    }
    
    /// <summary>
    /// 获取强类型的元数据
    /// </summary>
    /// <returns>LLM提供商元数据</returns>
    public override LLMProviderMetadata GetTypedMetadata()
    {
        return new LLMProviderMetadata
        {
            Value = Identifier,
            Description = Description,
            Provider = _provider,
            DisplayName = _displayName,
            Performance = _performance,
            UseCase = _useCase,
            IsEnterprise = _isEnterprise,
            IsEmbedding = _isEmbedding,
            PricingTier = _pricingTier,
            Features = _features.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim()).ToArray(),
            Icon = _icon,
            Category = _category
        };
    }
}