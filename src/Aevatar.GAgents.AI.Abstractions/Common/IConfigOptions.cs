using System;
using System.Collections.Generic;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// 配置选项接口，提供类型安全的选项管理
/// </summary>
/// <typeparam name="T">选项值类型</typeparam>
public interface IConfigOptions<T>
{
    /// <summary>
    /// 默认值
    /// </summary>
    T DefaultValue { get; }
    
    /// <summary>
    /// 可用选项列表
    /// </summary>
    IReadOnlyList<T> AvailableOptions { get; }
    
    /// <summary>
    /// 验证选项是否有效
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <returns>是否有效</returns>
    bool IsValidOption(T value);
    
    /// <summary>
    /// 获取选项描述（可选）
    /// </summary>
    /// <param name="value">选项值</param>
    /// <returns>描述信息</returns>
    string GetDescription(T value);
}

/// <summary>
/// 配置选项元数据，用于运行时获取选项信息
/// </summary>
public class ConfigOptionMetadata
{
    public string PropertyName { get; set; } = string.Empty;
    public Type PropertyType { get; set; } = typeof(object);
    public object DefaultValue { get; set; } = new();
    public IReadOnlyList<object> AvailableOptions { get; set; } = new List<object>();
    public string Description { get; set; } = string.Empty;
    public Dictionary<object, string> OptionDescriptions { get; set; } = new();
}

/// <summary>
/// 配置选项提供器接口，用于获取配置的所有选项信息
/// </summary>
public interface IConfigOptionsProvider
{
    /// <summary>
    /// 获取指定配置类型的所有选项元数据
    /// </summary>
    /// <param name="configType">配置类型</param>
    /// <returns>选项元数据字典</returns>
    Dictionary<string, ConfigOptionMetadata> GetOptionsMetadata(Type configType);
    
    /// <summary>
    /// 获取指定配置类型和属性的选项信息
    /// </summary>
    /// <param name="configType">配置类型</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>选项元数据</returns>
    ConfigOptionMetadata? GetPropertyOptions(Type configType, string propertyName);
} 