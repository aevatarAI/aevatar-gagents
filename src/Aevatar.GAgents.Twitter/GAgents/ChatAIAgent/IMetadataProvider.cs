using System;
using System.Collections.Generic;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// 元数据提供者接口
/// 定义获取类型元数据的通用契约
/// </summary>
/// <typeparam name="T">元数据数据类型</typeparam>
public interface IMetadataProvider<out T>
{
    /// <summary>
    /// 获取元数据
    /// </summary>
    /// <returns>元数据对象</returns>
    T GetMetadata();
}

/// <summary>
/// 枚举元数据提供者接口
/// 专门为枚举类型提供元数据
/// </summary>
/// <typeparam name="TEnum">枚举类型</typeparam>
/// <typeparam name="TMetadata">元数据类型</typeparam>
public interface IEnumMetadataProvider<TEnum, TMetadata> where TEnum : Enum
{
    /// <summary>
    /// 获取指定枚举值的元数据
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>元数据对象</returns>
    TMetadata GetMetadata(TEnum enumValue);
    
    /// <summary>
    /// 获取所有枚举值的元数据
    /// </summary>
    /// <returns>所有枚举值的元数据字典</returns>
    Dictionary<TEnum, TMetadata> GetAllMetadata();
}

/// <summary>
/// Schema增强接口
/// 定义如何将元数据转换为Schema扩展
/// </summary>
public interface ISchemaEnhancer
{
    /// <summary>
    /// 检查类型是否支持Schema增强
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>是否支持增强</returns>
    bool CanEnhance(Type type);
    
    /// <summary>
    /// 为类型生成增强的Schema
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>增强的Schema对象</returns>
    object? EnhanceSchema(Type type);
}

/// <summary>
/// 属性Schema增强接口
/// 为类的特定属性提供Schema增强
/// </summary>
public interface IPropertySchemaEnhancer
{
    /// <summary>
    /// 检查属性是否支持Schema增强
    /// </summary>
    /// <param name="propertyType">属性类型</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>是否支持增强</returns>
    bool CanEnhanceProperty(Type propertyType, string propertyName);
    
    /// <summary>
    /// 为属性生成增强的Schema
    /// </summary>
    /// <param name="propertyType">属性类型</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>增强的Schema对象</returns>
    object? EnhancePropertySchema(Type propertyType, string propertyName);
}