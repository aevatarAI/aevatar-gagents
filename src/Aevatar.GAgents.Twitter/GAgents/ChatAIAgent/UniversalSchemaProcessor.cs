using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// 通用Schema处理器
/// 为任何带有元数据特性的类型生成增强的Schema
/// </summary>
public static class UniversalSchemaProcessor
{
    /// <summary>
    /// Schema缓存，避免重复处理
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object?> _schemaCache = new();
    
    /// <summary>
    /// 属性Schema缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, object?> _propertySchemaCache = new();

    /// <summary>
    /// 检查类型是否有元数据特性
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>是否有元数据特性</returns>
    public static bool HasMetadataAttributes(Type type)
    {
        if (type.IsEnum)
        {
            return HasEnumMetadataAttributes(type);
        }
        
        // 检查类型本身是否有元数据特性
        return type.GetCustomAttributes<MetadataAttributeBase>().Any();
    }
    
    /// <summary>
    /// 检查枚举是否有元数据特性
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>是否有元数据特性</returns>
    public static bool HasEnumMetadataAttributes(Type enumType)
    {
        if (!enumType.IsEnum) return false;
        
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        return fields.Any(field => field.GetCustomAttributes<MetadataAttributeBase>().Any());
    }
    
    /// <summary>
    /// 为类型生成增强的Schema
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="useCache">是否使用缓存</param>
    /// <returns>增强的Schema对象</returns>
    public static object? ProcessType(Type type, bool useCache = true)
    {
        if (useCache && _schemaCache.TryGetValue(type, out var cachedSchema))
        {
            return cachedSchema;
        }
        
        object? schema = null;
        
        if (type.IsEnum)
        {
            schema = ProcessEnum(type);
        }
        else
        {
            schema = ProcessClass(type);
        }
        
        if (useCache && schema != null)
        {
            _schemaCache.TryAdd(type, schema);
        }
        
        return schema;
    }
    
    /// <summary>
    /// 处理枚举类型
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>增强的枚举Schema</returns>
    private static object? ProcessEnum(Type enumType)
    {
        if (!HasEnumMetadataAttributes(enumType))
        {
            return null;
        }
        
        var enumValues = Enum.GetValues(enumType);
        var enumNames = Enum.GetNames(enumType);
        var enumIntegers = enumValues.Cast<object>().Select(v => Convert.ToInt32(v)).ToArray();
        
        // 提取Description特性
        var descriptions = ExtractEnumDescriptions(enumType, enumNames);
        
        // 提取元数据特性
        var metadata = ExtractEnumMetadata(enumType, enumNames);
        
        // 生成UI配置
        var uiConfig = GenerateEnumUIConfig(enumType, metadata);
        
        // 生成分组信息
        var groups = GenerateEnumGroups(metadata);
        
        var schema = new
        {
            type = "integer",
            @enum = enumIntegers,
            title = GetEnumTitle(enumType),
            description = GetEnumDescription(enumType),
            
            // 标准扩展
            xEnumNames = enumNames,
            xEnumDescriptions = descriptions,
            
            // 元数据扩展
            xEnumMetadata = metadata,
            
            // UI配置扩展
            xUIConfig = uiConfig,
            
            // 分组信息
            xGroups = groups
        };
        
        return schema;
    }
    
    /// <summary>
    /// 处理类类型
    /// </summary>
    /// <param name="classType">类类型</param>
    /// <returns>增强的类Schema</returns>
    private static object? ProcessClass(Type classType)
    {
        var classMetadata = classType.GetCustomAttributes<MetadataAttributeBase>().ToArray();
        
        if (!classMetadata.Any())
        {
            return null;
        }
        
        var schema = new
        {
            type = "object",
            title = classType.Name,
            description = GetClassDescription(classType),
            
            // 类级别的元数据
            xClassMetadata = classMetadata.Select(attr => new
            {
                category = attr.Category,
                version = attr.Version,
                metadata = attr.GetMetadata()
            }).ToArray()
        };
        
        return schema;
    }
    
    /// <summary>
    /// 提取枚举的Description特性
    /// </summary>
    private static string[] ExtractEnumDescriptions(Type enumType, string[] enumNames)
    {
        return enumNames.Select(name =>
        {
            var field = enumType.GetField(name);
            var descriptionAttr = field?.GetCustomAttribute<DescriptionAttribute>();
            return descriptionAttr?.Description ?? name;
        }).ToArray();
    }
    
    /// <summary>
    /// 提取枚举的元数据特性
    /// </summary>
    private static object[] ExtractEnumMetadata(Type enumType, string[] enumNames)
    {
        return enumNames.Select(name =>
        {
            var field = enumType.GetField(name);
            var metadataAttrs = field?.GetCustomAttributes<MetadataAttributeBase>().ToArray() ?? Array.Empty<MetadataAttributeBase>();
            
            var enumValue = Enum.Parse(enumType, name);
            
            var result = new
            {
                value = Convert.ToInt32(enumValue),
                name = name,
                hasMetadata = metadataAttrs.Any(),
                metadata = metadataAttrs.Any() ? metadataAttrs.Select(attr => new
                {
                    category = attr.Category,
                    version = attr.Version,
                    type = attr.GetMetadataType().Name,
                    data = attr.GetMetadata()
                }).ToArray() : null
            };
            
            return result;
        }).ToArray();
    }
    
    /// <summary>
    /// 生成枚举UI配置
    /// </summary>
    private static object GenerateEnumUIConfig(Type enumType, object[] metadata)
    {
        var hasMetadata = metadata.Cast<dynamic>().Any(m => m.hasMetadata);
        
        return new
        {
            displayFormat = hasMetadata ? "enhanced-dropdown" : "simple-dropdown",
            showMetadata = hasMetadata,
            showDescriptions = true,
            enableSearch = hasMetadata,
            enableGrouping = hasMetadata,
            defaultValue = GetEnumDefaultValue(enumType)
        };
    }
    
    /// <summary>
    /// 生成枚举分组信息
    /// </summary>
    private static object GenerateEnumGroups(object[] metadata)
    {
        var metadataList = metadata.Cast<dynamic>().ToList();
        
        // 按类别分组
        var byCategory = metadataList
            .Where(m => m.hasMetadata)
            .SelectMany(m => ((object[])m.metadata).Cast<dynamic>())
            .GroupBy(attr => (string)attr.category)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    displayName = g.Key,
                    count = g.Count()
                }
            );
        
        return new
        {
            byCategory = byCategory.Any() ? byCategory : null,
            hasGroups = byCategory.Any()
        };
    }
    
    /// <summary>
    /// 获取枚举标题
    /// </summary>
    private static string GetEnumTitle(Type enumType)
    {
        var displayNameAttr = enumType.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
        return displayNameAttr?.DisplayName ?? enumType.Name;
    }
    
    /// <summary>
    /// 获取枚举描述
    /// </summary>
    private static string GetEnumDescription(Type enumType)
    {
        var descriptionAttr = enumType.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttr?.Description ?? $"Enumeration of {enumType.Name} values";
    }
    
    /// <summary>
    /// 获取类描述
    /// </summary>
    private static string GetClassDescription(Type classType)
    {
        var descriptionAttr = classType.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttr?.Description ?? $"Configuration class {classType.Name}";
    }
    
    /// <summary>
    /// 获取枚举默认值
    /// </summary>
    private static object? GetEnumDefaultValue(Type enumType)
    {
        var defaultValueAttr = enumType.GetCustomAttribute<System.ComponentModel.DefaultValueAttribute>();
        if (defaultValueAttr?.Value != null)
        {
            return Convert.ToInt32(defaultValueAttr.Value);
        }
        
        // 返回第一个枚举值
        var values = Enum.GetValues(enumType);
        return values.Length > 0 ? Convert.ToInt32(values.GetValue(0)) : null;
    }
    
    /// <summary>
    /// 获取JSON格式的增强Schema
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="useCache">是否使用缓存</param>
    /// <returns>JSON字符串</returns>
    public static string? GetSchemaJson(Type type, bool useCache = true)
    {
        var schema = ProcessType(type, useCache);
        if (schema == null) return null;
        
        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
    
    /// <summary>
    /// 清除缓存
    /// </summary>
    public static void ClearCache()
    {
        _schemaCache.Clear();
        _propertySchemaCache.Clear();
    }
    
    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计</returns>
    public static object GetCacheStats()
    {
        return new
        {
            typeSchemaCount = _schemaCache.Count,
            propertySchemaCount = _propertySchemaCache.Count,
            totalCacheSize = _schemaCache.Count + _propertySchemaCache.Count
        };
    }
}