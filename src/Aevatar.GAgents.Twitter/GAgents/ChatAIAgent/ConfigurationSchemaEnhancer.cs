using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

/// <summary>
/// 配置Schema增强器
/// 为整个配置类提供增强的Schema，包括所有属性的元数据
/// </summary>
public static class ConfigurationSchemaEnhancer
{
    /// <summary>
    /// 配置Schema缓存
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object> _configSchemaCache = new();
    
    /// <summary>
    /// 属性Schema缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, object> _propertySchemaCache = new();

    /// <summary>
    /// 为配置类生成完整的增强Schema
    /// Station调用此方法获取包含所有属性元数据的Schema
    /// </summary>
    /// <typeparam name="T">配置类类型</typeparam>
    /// <param name="useCache">是否使用缓存</param>
    /// <returns>增强的配置Schema</returns>
    public static object GetEnhancedSchema<T>(bool useCache = true) where T : class
    {
        return GetEnhancedSchema(typeof(T), useCache);
    }
    
    /// <summary>
    /// 为配置类生成完整的增强Schema
    /// </summary>
    /// <param name="configurationType">配置类类型</param>
    /// <param name="useCache">是否使用缓存</param>
    /// <returns>增强的配置Schema</returns>
    public static object GetEnhancedSchema(Type configurationType, bool useCache = true)
    {
        if (useCache && _configSchemaCache.TryGetValue(configurationType, out var cachedSchema))
        {
            return cachedSchema;
        }
        
        var properties = configurationType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var enhancedProperties = new Dictionary<string, object>();
        var propertyMetadata = new Dictionary<string, object>();
        
        foreach (var property in properties)
        {
            var propertySchema = ProcessProperty(property);
            if (propertySchema != null)
            {
                enhancedProperties[property.Name] = propertySchema;
                
                // 提取属性级别的元数据
                var metadata = ExtractPropertyMetadata(property);
                if (metadata != null)
                {
                    propertyMetadata[property.Name] = metadata;
                }
            }
        }
        
        var schema = new
        {
            type = "object",
            title = GetConfigurationTitle(configurationType),
            description = GetConfigurationDescription(configurationType),
            
            // 基础属性Schema
            properties = enhancedProperties,
            
            // 配置级别的元数据
            xConfigurationMetadata = ExtractConfigurationMetadata(configurationType),
            
            // 属性级别的元数据
            xPropertyMetadata = propertyMetadata.Any() ? propertyMetadata : null,
            
            // UI配置
            xUIConfig = GenerateConfigurationUIConfig(configurationType, enhancedProperties),
            
            // 验证规则
            xValidationRules = GenerateValidationRules(configurationType, properties),
            
            // 分组信息
            xPropertyGroups = GeneratePropertyGroups(properties, propertyMetadata)
        };
        
        if (useCache)
        {
            _configSchemaCache.TryAdd(configurationType, schema);
        }
        
        return schema;
    }
    
    /// <summary>
    /// 为单个属性生成增强Schema
    /// </summary>
    /// <typeparam name="T">配置类类型</typeparam>
    /// <param name="propertyName">属性名称</param>
    /// <param name="useCache">是否使用缓存</param>
    /// <returns>属性的增强Schema</returns>
    public static object? GetPropertyEnhancedSchema<T>(string propertyName, bool useCache = true) where T : class
    {
        return GetPropertyEnhancedSchema(typeof(T), propertyName, useCache);
    }
    
    /// <summary>
    /// 为单个属性生成增强Schema
    /// </summary>
    /// <param name="configurationType">配置类类型</param>
    /// <param name="propertyName">属性名称</param>
    /// <param name="useCache">是否使用缓存</param>
    /// <returns>属性的增强Schema</returns>
    public static object? GetPropertyEnhancedSchema(Type configurationType, string propertyName, bool useCache = true)
    {
        var cacheKey = $"{configurationType.FullName}.{propertyName}";
        
        if (useCache && _propertySchemaCache.TryGetValue(cacheKey, out var cachedSchema))
        {
            return cachedSchema;
        }
        
        var property = configurationType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null) return null;
        
        var schema = ProcessProperty(property);
        
        if (useCache && schema != null)
        {
            _propertySchemaCache.TryAdd(cacheKey, schema);
        }
        
        return schema;
    }
    
    /// <summary>
    /// 处理单个属性
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>属性的增强Schema</returns>
    private static object? ProcessProperty(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        
        // 处理可空类型
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            propertyType = propertyType.GetGenericArguments()[0];
        }
        
        // 使用通用Schema处理器
        var enhancedSchema = UniversalSchemaProcessor.ProcessType(propertyType, useCache: true);
        
        if (enhancedSchema != null)
        {
            return enhancedSchema;
        }
        
        // 如果没有元数据，返回基础Schema信息
        return GenerateBasicPropertySchema(property);
    }
    
    /// <summary>
    /// 生成基础属性Schema
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>基础Schema</returns>
    private static object GenerateBasicPropertySchema(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        
        // 处理可空类型
        var isNullable = false;
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            propertyType = propertyType.GetGenericArguments()[0];
            isNullable = true;
        }
        
        var schema = new Dictionary<string, object>();
        
        if (propertyType == typeof(string))
        {
            schema["type"] = isNullable ? new[] { "null", "string" } : "string";
        }
        else if (propertyType == typeof(int) || propertyType == typeof(long))
        {
            schema["type"] = "integer";
            if (isNullable) schema["type"] = new[] { "null", "integer" };
        }
        else if (propertyType == typeof(bool))
        {
            schema["type"] = "boolean";
            if (isNullable) schema["type"] = new[] { "null", "boolean" };
        }
        else if (propertyType.IsEnum)
        {
            schema["type"] = "integer";
            schema["enum"] = Enum.GetValues(propertyType).Cast<object>().Select(v => Convert.ToInt32(v)).ToArray();
            schema["x-enumNames"] = Enum.GetNames(propertyType);
            if (isNullable) schema["type"] = new[] { "null", "integer" };
        }
        else if (propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>)))
        {
            schema["type"] = "array";
            // 可以进一步处理数组项的类型
        }
        else
        {
            schema["type"] = "object";
        }
        
        // 添加基础属性信息
        schema["title"] = property.Name;
        schema["description"] = GetPropertyDescription(property);
        
        return schema;
    }
    
    /// <summary>
    /// 提取配置类级别的元数据
    /// </summary>
    /// <param name="configurationType">配置类类型</param>
    /// <returns>配置元数据</returns>
    private static object? ExtractConfigurationMetadata(Type configurationType)
    {
        var metadataAttrs = configurationType.GetCustomAttributes<MetadataAttributeBase>().ToArray();
        
        if (!metadataAttrs.Any()) return null;
        
        return metadataAttrs.Select(attr => new
        {
            category = attr.Category,
            version = attr.Version,
            type = attr.GetMetadataType().Name,
            metadata = attr.GetMetadata()
        }).ToArray();
    }
    
    /// <summary>
    /// 提取属性级别的元数据
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>属性元数据</returns>
    private static object? ExtractPropertyMetadata(PropertyInfo property)
    {
        var metadataAttrs = property.GetCustomAttributes<MetadataAttributeBase>().ToArray();
        
        if (!metadataAttrs.Any()) return null;
        
        return new
        {
            propertyName = property.Name,
            propertyType = property.PropertyType.Name,
            metadata = metadataAttrs.Select(attr => new
            {
                category = attr.Category,
                version = attr.Version,
                type = attr.GetMetadataType().Name,
                data = attr.GetMetadata()
            }).ToArray()
        };
    }
    
    /// <summary>
    /// 生成配置UI配置
    /// </summary>
    /// <param name="configurationType">配置类类型</param>
    /// <param name="enhancedProperties">增强的属性Schema</param>
    /// <returns>UI配置</returns>
    private static object GenerateConfigurationUIConfig(Type configurationType, Dictionary<string, object> enhancedProperties)
    {
        var hasEnhancedProperties = enhancedProperties.Values.Any(p => 
        {
            var dict = p as Dictionary<string, object>;
            return dict?.ContainsKey("xEnumMetadata") == true || dict?.ContainsKey("xUIConfig") == true;
        });
        
        return new
        {
            layoutType = hasEnhancedProperties ? "enhanced-form" : "simple-form",
            showPropertyHelp = true,
            enableAdvancedMode = hasEnhancedProperties,
            groupByCategory = true,
            enableSearch = enhancedProperties.Count > 5,
            enableValidation = true
        };
    }
    
    /// <summary>
    /// 生成验证规则
    /// </summary>
    /// <param name="configurationType">配置类类型</param>
    /// <param name="properties">属性列表</param>
    /// <returns>验证规则</returns>
    private static object GenerateValidationRules(Type configurationType, PropertyInfo[] properties)
    {
        var rules = new Dictionary<string, object>();
        
        foreach (var property in properties)
        {
            var propertyRules = new List<object>();
            
            // 检查必需属性
            var requiredAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();
            if (requiredAttr != null)
            {
                propertyRules.Add(new { type = "required", message = requiredAttr.ErrorMessage ?? $"{property.Name} is required" });
            }
            
            // 检查字符串长度
            var stringLengthAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                propertyRules.Add(new { 
                    type = "stringLength", 
                    maxLength = stringLengthAttr.MaximumLength,
                    minLength = stringLengthAttr.MinimumLength,
                    message = stringLengthAttr.ErrorMessage ?? $"{property.Name} length is invalid"
                });
            }
            
            // 检查范围
            var rangeAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RangeAttribute>();
            if (rangeAttr != null)
            {
                propertyRules.Add(new { 
                    type = "range", 
                    minimum = rangeAttr.Minimum,
                    maximum = rangeAttr.Maximum,
                    message = rangeAttr.ErrorMessage ?? $"{property.Name} is out of range"
                });
            }
            
            if (propertyRules.Any())
            {
                rules[property.Name] = propertyRules;
            }
        }
        
        return rules.Any() ? rules : null;
    }
    
    /// <summary>
    /// 生成属性分组信息
    /// </summary>
    /// <param name="properties">属性列表</param>
    /// <param name="propertyMetadata">属性元数据</param>
    /// <returns>分组信息</returns>
    private static object? GeneratePropertyGroups(PropertyInfo[] properties, Dictionary<string, object> propertyMetadata)
    {
        if (!propertyMetadata.Any()) return null;
        
        var groups = new Dictionary<string, object>();
        
        // 按属性类型分组
        var byType = properties.GroupBy(p => GetPropertyCategory(p))
            .ToDictionary(g => g.Key, g => g.Select(p => p.Name).ToArray());
        
        if (byType.Any())
        {
            groups["byType"] = byType;
        }
        
        // 按元数据类别分组
        var byMetadataCategory = propertyMetadata
            .GroupBy(kvp =>
            {
                var metadata = kvp.Value as dynamic;
                return metadata?.metadata?[0]?.category?.ToString() ?? "Other";
            })
            .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToArray());
        
        if (byMetadataCategory.Any())
        {
            groups["byMetadataCategory"] = byMetadataCategory;
        }
        
        return groups.Any() ? groups : null;
    }
    
    /// <summary>
    /// 获取属性类别
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>属性类别</returns>
    private static string GetPropertyCategory(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        
        if (propertyType.IsEnum) return "Enum";
        if (propertyType == typeof(string)) return "String";
        if (propertyType.IsValueType) return "Value";
        if (propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))) return "Collection";
        
        return "Object";
    }
    
    /// <summary>
    /// 获取配置类标题
    /// </summary>
    /// <param name="configurationType">配置类类型</param>
    /// <returns>标题</returns>
    private static string GetConfigurationTitle(Type configurationType)
    {
        var displayNameAttr = configurationType.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
        return displayNameAttr?.DisplayName ?? configurationType.Name;
    }
    
    /// <summary>
    /// 获取配置类描述
    /// </summary>
    /// <param name="configurationType">配置类类型</param>
    /// <returns>描述</returns>
    private static string GetConfigurationDescription(Type configurationType)
    {
        var descriptionAttr = configurationType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        return descriptionAttr?.Description ?? $"Configuration settings for {configurationType.Name}";
    }
    
    /// <summary>
    /// 获取属性描述
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>描述</returns>
    private static string GetPropertyDescription(PropertyInfo property)
    {
        var descriptionAttr = property.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        return descriptionAttr?.Description ?? $"Configuration property {property.Name}";
    }
    
    /// <summary>
    /// 获取JSON格式的增强Schema
    /// </summary>
    /// <typeparam name="T">配置类类型</typeparam>
    /// <param name="useCache">是否使用缓存</param>
    /// <returns>JSON字符串</returns>
    public static string GetEnhancedSchemaJson<T>(bool useCache = true) where T : class
    {
        var schema = GetEnhancedSchema<T>(useCache);
        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
    
    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public static void ClearCache()
    {
        _configSchemaCache.Clear();
        _propertySchemaCache.Clear();
        UniversalSchemaProcessor.ClearCache();
    }
    
    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计</returns>
    public static object GetCacheStats()
    {
        var universalStats = UniversalSchemaProcessor.GetCacheStats() as dynamic;
        
        return new
        {
            configurationSchemas = _configSchemaCache.Count,
            propertySchemas = _propertySchemaCache.Count,
            universalSchemas = universalStats?.totalCacheSize ?? 0,
            totalCacheSize = _configSchemaCache.Count + _propertySchemaCache.Count + (universalStats?.totalCacheSize ?? 0)
        };
    }
}