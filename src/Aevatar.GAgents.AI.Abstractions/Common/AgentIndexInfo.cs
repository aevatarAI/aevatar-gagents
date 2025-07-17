using System;
using System.Collections.Generic;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// Agent索引信息，用于HTTP服务向LLM提供Agent信息
/// </summary>
public class AgentIndexInfo
{
    /// <summary>
    /// Agent唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 分类
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// L1描述 - 快速匹配用
    /// </summary>
    public string L1Description { get; set; } = string.Empty;
    
    /// <summary>
    /// L2描述 - 详细能力说明
    /// </summary>
    public string L2Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 能力列表
    /// </summary>
    public List<string> Capabilities { get; set; } = new();
    
    /// <summary>
    /// 标签
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// 输入格式
    /// </summary>
    public string InputFormat { get; set; } = "text";
    
    /// <summary>
    /// 输出格式
    /// </summary>
    public string OutputFormat { get; set; } = "text";
    
    /// <summary>
    /// 使用示例
    /// </summary>
    public string UsageExample { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent类型信息
    /// </summary>
    public string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent接口类型
    /// </summary>
    public string InterfaceType { get; set; } = string.Empty;
} 