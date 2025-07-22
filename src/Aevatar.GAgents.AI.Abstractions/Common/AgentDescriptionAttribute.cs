using System;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// Agent描述标记，用于HTTP服务扫描和LLM处理
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AgentDescriptionAttribute : Attribute
{
    /// <summary>
    /// Agent名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// L1描述 - 100-150字符快速描述，用于LLM快速匹配
    /// </summary>
    public string L1Description { get; set; } = string.Empty;
    
    /// <summary>
    /// L2描述 - 300-500字符详细能力说明，用于LLM详细理解
    /// </summary>
    public string L2Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 分类 (如: AI, Blockchain, Social, Chat, Workflow)
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// 能力列表，用于LLM理解Agent可执行的操作
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// 标签，便于LLM理解和分类
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// 输入格式说明 (如: "text", "json", "structured")
    /// </summary>
    public string InputFormat { get; set; } = "text";
    
    /// <summary>
    /// 输出格式说明 (如: "text", "json", "structured")
    /// </summary>
    public string OutputFormat { get; set; } = "text";
    
    /// <summary>
    /// 使用示例，为LLM提供调用参考
    /// </summary>
    public string UsageExample { get; set; } = string.Empty;
    
    public AgentDescriptionAttribute()
    {
    }
    
    public AgentDescriptionAttribute(string name, string l1Description)
    {
        Name = name;
        L1Description = l1Description;
    }
    
    public AgentDescriptionAttribute(string name, string l1Description, string l2Description)
    {
        Name = name;
        L1Description = l1Description;
        L2Description = l2Description;
    }
} 