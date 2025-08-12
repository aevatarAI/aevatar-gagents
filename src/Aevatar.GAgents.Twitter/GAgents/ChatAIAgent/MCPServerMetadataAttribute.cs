using System;
using System.Linq;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// MCP服务器元数据信息
/// </summary>
public class MCPServerMetadata
{
    /// <summary>
    /// 服务器类型标识值
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// 详细描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 连接方式
    /// </summary>
    public string ConnectionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 性能特点
    /// </summary>
    public string Performance { get; set; } = string.Empty;
    
    /// <summary>
    /// 使用场景
    /// </summary>
    public string UseCase { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否支持流式传输
    /// </summary>
    public bool SupportsStreaming { get; set; }
    
    /// <summary>
    /// 是否需要认证
    /// </summary>
    public bool RequiresAuth { get; set; }
    
    /// <summary>
    /// 复杂度级别
    /// </summary>
    public string ComplexityLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// 支持的协议
    /// </summary>
    public string[] SupportedProtocols { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// 图标名称
    /// </summary>
    public string Icon { get; set; } = string.Empty;
    
    /// <summary>
    /// 推荐度（1-5星）
    /// </summary>
    public int RecommendationLevel { get; set; } = 3;
}

/// <summary>
/// MCP服务器元数据特性
/// 为MCP服务器类型枚举提供结构化的元数据
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class MCPServerMetadataAttribute : EnumMetadataAttributeBase<MCPServerMetadata>
{
    private readonly string _connectionType;
    private readonly string _performance;
    private readonly string _useCase;
    private readonly bool _supportsStreaming;
    private readonly bool _requiresAuth;
    private readonly string _complexityLevel;
    private readonly string _supportedProtocols;
    private readonly string _icon;
    private readonly int _recommendationLevel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MCPServerMetadataAttribute(
        string displayName,
        string connectionType,
        string performance,
        string useCase,
        bool supportsStreaming = false,
        bool requiresAuth = false,
        string complexityLevel = "Medium",
        string supportedProtocols = "",
        string icon = "",
        int recommendationLevel = 3)
    {
        _connectionType = connectionType;
        _performance = performance;
        _useCase = useCase;
        _supportsStreaming = supportsStreaming;
        _requiresAuth = requiresAuth;
        _complexityLevel = complexityLevel;
        _supportedProtocols = supportedProtocols;
        _icon = icon;
        _recommendationLevel = recommendationLevel;
        
        // 设置基类属性
        Category = "MCPServer";
        DisplayName = displayName;
        Description = $"{connectionType} connection type for {useCase}";
    }
    
    /// <summary>
    /// 获取强类型的元数据
    /// </summary>
    /// <returns>MCP服务器元数据</returns>
    public override MCPServerMetadata GetTypedMetadata()
    {
        return new MCPServerMetadata
        {
            Value = Identifier,
            Description = Description,
            DisplayName = DisplayName,
            ConnectionType = _connectionType,
            Performance = _performance,
            UseCase = _useCase,
            SupportsStreaming = _supportsStreaming,
            RequiresAuth = _requiresAuth,
            ComplexityLevel = _complexityLevel,
            SupportedProtocols = _supportedProtocols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).ToArray(),
            Icon = _icon,
            RecommendationLevel = _recommendationLevel
        };
    }
}