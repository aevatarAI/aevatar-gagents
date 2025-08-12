using System;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// 元数据特性基类
/// 所有元数据特性的通用基础
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
public abstract class MetadataAttributeBase : Attribute, IMetadataProvider<object>
{
    /// <summary>
    /// 元数据类别
    /// </summary>
    public virtual string Category { get; protected set; } = "General";
    
    /// <summary>
    /// 元数据版本
    /// </summary>
    public virtual string Version { get; protected set; } = "1.0";
    
    /// <summary>
    /// 是否启用缓存
    /// </summary>
    public virtual bool CacheEnabled { get; protected set; } = true;
    
    /// <summary>
    /// 获取元数据对象
    /// </summary>
    /// <returns>元数据对象</returns>
    public abstract object GetMetadata();
    
    /// <summary>
    /// 获取元数据的JSON表示
    /// </summary>
    /// <returns>JSON字符串</returns>
    public virtual string GetMetadataJson()
    {
        var metadata = GetMetadata();
        return System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
    
    /// <summary>
    /// 获取元数据类型
    /// </summary>
    /// <returns>元数据的类型</returns>
    public abstract Type GetMetadataType();
    
    /// <summary>
    /// 验证元数据是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public virtual bool ValidateMetadata()
    {
        try
        {
            var metadata = GetMetadata();
            return metadata != null;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 强类型元数据特性基类
/// </summary>
/// <typeparam name="T">元数据类型</typeparam>
public abstract class MetadataAttributeBase<T> : MetadataAttributeBase, IMetadataProvider<T>
{
    /// <summary>
    /// 获取强类型的元数据
    /// </summary>
    /// <returns>强类型元数据对象</returns>
    public abstract T GetTypedMetadata();
    
    /// <summary>
    /// 实现基类的GetMetadata方法
    /// </summary>
    /// <returns>元数据对象</returns>
    public override object GetMetadata()
    {
        return GetTypedMetadata() ?? new object();
    }
    
    /// <summary>
    /// 获取元数据类型
    /// </summary>
    /// <returns>元数据的类型</returns>
    public override Type GetMetadataType()
    {
        return typeof(T);
    }
    
    /// <summary>
    /// 强类型的元数据提供者实现
    /// </summary>
    /// <returns>强类型元数据</returns>
    T IMetadataProvider<T>.GetMetadata()
    {
        return GetTypedMetadata();
    }
}

/// <summary>
/// 枚举元数据特性基类
/// 专门为枚举字段提供元数据
/// </summary>
/// <typeparam name="T">元数据类型</typeparam>
public abstract class EnumMetadataAttributeBase<T> : MetadataAttributeBase<T>
{
    /// <summary>
    /// 元数据类别默认为枚举
    /// </summary>
    public override string Category { get; protected set; } = "Enum";
    
    /// <summary>
    /// 枚举值的标识符
    /// </summary>
    public virtual string Identifier { get; protected set; } = string.Empty;
    
    /// <summary>
    /// 枚举值的显示名称
    /// </summary>
    public virtual string DisplayName { get; protected set; } = string.Empty;
    
    /// <summary>
    /// 枚举值的描述
    /// </summary>
    public virtual string Description { get; protected set; } = string.Empty;
    
    /// <summary>
    /// 是否已弃用
    /// </summary>
    public virtual bool IsDeprecated { get; protected set; } = false;
    
    /// <summary>
    /// 排序权重
    /// </summary>
    public virtual int SortOrder { get; protected set; } = 0;
}