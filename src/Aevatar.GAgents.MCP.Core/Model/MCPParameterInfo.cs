using Orleans;
using System.Text.Json;

namespace Aevatar.GAgents.MCP.Core.Model;

/// <summary>
/// 增强的MCP参数信息，支持完整JsonSchema到Orleans State到KernelParameterMetadata的转换
/// </summary>
[GenerateSerializer]
public class MCPParameterInfo
{
    #region 基础属性（向后兼容）
    /// <summary>
    /// 参数名称
    /// </summary>
    [Id(0)] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 参数类型（简化表示，保持向后兼容）
    /// </summary>
    [Id(1)] public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// 参数描述
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否必需参数
    /// </summary>
    [Id(3)] public bool Required { get; set; }
    
    /// <summary>
    /// 默认值
    /// </summary>
    [Id(4)] public object? DefaultValue { get; set; }
    #endregion

    #region JsonSchema扩展属性
    /// <summary>
    /// 完整的JsonSchema原始数据（序列化为字符串存储）
    /// </summary>
    [Id(5)] public string? RawJsonSchema { get; set; }
    
    /// <summary>
    /// JsonSchema格式约束（如date-time, email等）
    /// </summary>
    [Id(6)] public string? Format { get; set; }
    
    /// <summary>
    /// 字符串最小长度
    /// </summary>
    [Id(7)] public int? MinLength { get; set; }
    
    /// <summary>
    /// 字符串最大长度
    /// </summary>
    [Id(8)] public int? MaxLength { get; set; }
    
    /// <summary>
    /// 数字最小值
    /// </summary>
    [Id(9)] public double? Minimum { get; set; }
    
    /// <summary>
    /// 数字最大值
    /// </summary>
    [Id(10)] public double? Maximum { get; set; }
    
    /// <summary>
    /// 正则表达式模式
    /// </summary>
    [Id(11)] public string? Pattern { get; set; }
    
    /// <summary>
    /// 枚举值列表
    /// </summary>
    [Id(12)] public List<string>? EnumValues { get; set; }
    
    /// <summary>
    /// 数组项类型信息（用于array类型）
    /// </summary>
    [Id(13)] public MCPParameterInfo? ArrayItems { get; set; }
    
    /// <summary>
    /// 对象属性信息（用于object类型）
    /// </summary>
    [Id(14)] public Dictionary<string, MCPParameterInfo>? ObjectProperties { get; set; }
    
    /// <summary>
    /// 对象必需属性列表
    /// </summary>
    [Id(15)] public List<string>? RequiredProperties { get; set; }
    
    /// <summary>
    /// 是否允许额外属性（对象类型）
    /// </summary>
    [Id(16)] public bool? AdditionalProperties { get; set; }
    
    /// <summary>
    /// JsonSchema类型的详细信息（支持联合类型如["string", "null"]）
    /// </summary>
    [Id(17)] public List<string>? TypeArray { get; set; }
    
    /// <summary>
    /// 示例值列表
    /// </summary>
    [Id(18)] public List<string>? Examples { get; set; }
    #endregion

    #region 静态工厂方法
    /// <summary>
    /// 从官方SDK的JsonElement创建MCPParameterInfo
    /// </summary>
    /// <param name="name">参数名</param>
    /// <param name="schema">JsonSchema元素</param>
    /// <param name="required">是否必需</param>
    /// <returns>MCPParameterInfo实例</returns>
    public static MCPParameterInfo FromJsonSchema(string name, JsonElement schema, bool required = false)
    {
        var paramInfo = new MCPParameterInfo
        {
            Name = name,
            Required = required,
            RawJsonSchema = schema.GetRawText()
        };

        // 解析基本类型
        if (schema.TryGetProperty("type", out var typeElement))
        {
            paramInfo.Type = GetPrimaryType(typeElement);
            paramInfo.TypeArray = GetTypeArray(typeElement);
        }

        // 解析描述
        if (schema.TryGetProperty("description", out var descElement))
        {
            paramInfo.Description = descElement.GetString() ?? string.Empty;
        }

        // 解析格式
        if (schema.TryGetProperty("format", out var formatElement))
        {
            paramInfo.Format = formatElement.GetString();
        }

        // 解析字符串约束
        if (schema.TryGetProperty("minLength", out var minLengthElement))
        {
            paramInfo.MinLength = minLengthElement.GetInt32();
        }
        if (schema.TryGetProperty("maxLength", out var maxLengthElement))
        {
            paramInfo.MaxLength = maxLengthElement.GetInt32();
        }
        if (schema.TryGetProperty("pattern", out var patternElement))
        {
            paramInfo.Pattern = patternElement.GetString();
        }

        // 解析数字约束
        if (schema.TryGetProperty("minimum", out var minimumElement))
        {
            paramInfo.Minimum = minimumElement.GetDouble();
        }
        if (schema.TryGetProperty("maximum", out var maximumElement))
        {
            paramInfo.Maximum = maximumElement.GetDouble();
        }

        // 解析枚举值
        if (schema.TryGetProperty("enum", out var enumElement) && enumElement.ValueKind == JsonValueKind.Array)
        {
            paramInfo.EnumValues = enumElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToList();
        }

        // 解析默认值
        if (schema.TryGetProperty("default", out var defaultElement))
        {
            paramInfo.DefaultValue = ConvertJsonElementToBasicType(defaultElement);
        }

        // 解析示例
        if (schema.TryGetProperty("examples", out var examplesElement) && examplesElement.ValueKind == JsonValueKind.Array)
        {
            paramInfo.Examples = examplesElement.EnumerateArray()
                .Select(e => e.GetRawText())
                .ToList();
        }

        // 解析数组项
        if (paramInfo.Type == "array" && schema.TryGetProperty("items", out var itemsElement))
        {
            paramInfo.ArrayItems = FromJsonSchema($"{name}_item", itemsElement);
        }

        // 解析对象属性
        if (paramInfo.Type == "object")
        {
            if (schema.TryGetProperty("properties", out var propertiesElement))
            {
                paramInfo.ObjectProperties = new Dictionary<string, MCPParameterInfo>();
                foreach (var prop in propertiesElement.EnumerateObject())
                {
                    paramInfo.ObjectProperties[prop.Name] = FromJsonSchema(prop.Name, prop.Value);
                }
            }

            if (schema.TryGetProperty("required", out var requiredElement) && requiredElement.ValueKind == JsonValueKind.Array)
            {
                paramInfo.RequiredProperties = requiredElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToList();
            }

            if (schema.TryGetProperty("additionalProperties", out var addPropsElement))
            {
                paramInfo.AdditionalProperties = addPropsElement.ValueKind == JsonValueKind.True;
            }
        }

        return paramInfo;
    }

    /// <summary>
    /// 从MCP工具JsonSchema的properties部分批量创建参数信息
    /// </summary>
    /// <param name="inputSchema">MCP工具的输入schema</param>
    /// <returns>参数信息字典</returns>
    public static Dictionary<string, MCPParameterInfo> FromMCPToolSchema(JsonElement inputSchema)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();

        if (inputSchema.ValueKind != JsonValueKind.Object)
            return parameters;

        // 获取必需参数列表
        var requiredParams = new HashSet<string>();
        if (inputSchema.TryGetProperty("required", out var requiredElement) && 
            requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in requiredElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    requiredParams.Add(item.GetString()!);
                }
            }
        }

        // 解析属性
        if (inputSchema.TryGetProperty("properties", out var propertiesElement))
        {
            foreach (var prop in propertiesElement.EnumerateObject())
            {
                var isRequired = requiredParams.Contains(prop.Name);
                parameters[prop.Name] = FromJsonSchema(prop.Name, prop.Value, isRequired);
            }
        }

        return parameters;
    }
    #endregion

    #region 转换方法
    /// <summary>
    /// 转换为Semantic Kernel的KernelParameterMetadata
    /// </summary>
    /// <returns>KernelParameterMetadata实例</returns>
    public object ToKernelParameterMetadata()
    {
        // 使用反射创建KernelParameterMetadata，因为构造函数可能有不同版本
        // 尝试不同的程序集名称（按优先级排序）
        var possibleAssemblyNames = new[]
        {
            "Microsoft.SemanticKernel.KernelParameterMetadata, Microsoft.SemanticKernel.Abstractions",
            "Microsoft.SemanticKernel.KernelParameterMetadata, Microsoft.SemanticKernel",
            "Microsoft.SemanticKernel.KernelParameterMetadata, Microsoft.SemanticKernel.Core"
        };
        
        Type? metadataType = null;
        foreach (var typeName in possibleAssemblyNames)
        {
            metadataType = System.Type.GetType(typeName);
            if (metadataType != null)
                break;
        }
        
        if (metadataType == null)
        {
            throw new InvalidOperationException("无法找到KernelParameterMetadata类型，尝试过以下程序集: " + string.Join(", ", possibleAssemblyNames));
        }

        // 尝试使用基本构造函数
        var constructors = metadataType.GetConstructors();
        var simpleConstructor = constructors.FirstOrDefault(c => 
            c.GetParameters().Length == 1 && 
            c.GetParameters()[0].ParameterType == typeof(string));

        if (simpleConstructor == null)
        {
            throw new InvalidOperationException("无法找到合适的KernelParameterMetadata构造函数");
        }

        // 对于数组类型，尝试使用带schema的构造函数
        if (Type == "array")
        {
            try
            {
                // 查找带schema参数的构造函数
                var schemaConstructor = constructors.FirstOrDefault(c =>
                {
                    var parameters = c.GetParameters();
                    return parameters.Length >= 2 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters.Any(p => p.ParameterType.Name == "KernelJsonSchema");
                });

                if (schemaConstructor != null)
                {
                    // 尝试创建KernelJsonSchema
                    var schemaBuilderType = System.Type.GetType("Microsoft.SemanticKernel.KernelJsonSchemaBuilder, Microsoft.SemanticKernel")
                                         ?? System.Type.GetType("Microsoft.SemanticKernel.KernelJsonSchemaBuilder, Microsoft.SemanticKernel.Abstractions");

                    if (schemaBuilderType != null)
                    {
                        var buildMethod = schemaBuilderType.GetMethod("Build", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (buildMethod != null)
                        {
                            var schema = GenerateJsonSchema();
                            var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema);
                            var kernelSchema = buildMethod.Invoke(null, new object[] { schemaJson });

                            var ctorParams = schemaConstructor.GetParameters();
                            var args = new object[ctorParams.Length];
                            args[0] = Name;

                            for (int i = 1; i < ctorParams.Length; i++)
                            {
                                if (ctorParams[i].ParameterType.Name == "KernelJsonSchema")
                                {
                                    args[i] = kernelSchema;
                                }
                                else if (ctorParams[i].HasDefaultValue)
                                {
                                    args[i] = ctorParams[i].DefaultValue;
                                }
                            }

                            var metadataWithSchema = schemaConstructor.Invoke(args);
                            
                            // 设置其他属性
                            SetPropertySafely(metadataWithSchema, "Description", Description);
                            SetPropertySafely(metadataWithSchema, "IsRequired", Required);
                            if (DefaultValue != null)
                            {
                                SetPropertySafely(metadataWithSchema, "DefaultValue", DefaultValue);
                            }
                            
                            return metadataWithSchema;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果失败，回退到默认方式
                Console.WriteLine($"Failed to create KernelParameterMetadata with schema: {ex.Message}");
            }
        }
        
        var metadata = simpleConstructor.Invoke([Name]);

        // 使用反射设置属性
        SetPropertySafely(metadata, "Description", Description);
        SetPropertySafely(metadata, "IsRequired", Required);
        
        // 尝试设置默认值
        if (DefaultValue != null)
        {
            SetPropertySafely(metadata, "DefaultValue", DefaultValue);
        }

        // 如果有类型信息，尝试设置
        if (!string.IsNullOrEmpty(Type))
        {
            SetPropertySafely(metadata, "ParameterType", GetDotNetType());
        }

        // 尝试设置Schema属性（用于支持复杂类型如数组）
        var schemaProp = metadataType.GetProperty("Schema");
        if (schemaProp != null && schemaProp.CanWrite)
        {
            try
            {
                var schema = GenerateJsonSchema();
                var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema);
                
                // 尝试创建KernelJsonSchema（Schema属性的实际类型）
                var kernelJsonSchemaType = System.Type.GetType("Microsoft.SemanticKernel.KernelJsonSchema, Microsoft.SemanticKernel.Abstractions")
                    ?? System.Type.GetType("Microsoft.SemanticKernel.KernelJsonSchema, Microsoft.SemanticKernel");
                
                if (kernelJsonSchemaType != null)
                {
                    // KernelJsonSchema有一个构造函数接受string参数
                    var ctor = kernelJsonSchemaType.GetConstructor(new Type[] { typeof(string) });
                    if (ctor != null)
                    {
                        var kernelJsonSchema = ctor.Invoke(new object[] { schemaJson });
                        schemaProp.SetValue(metadata, kernelJsonSchema);
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略错误，保持向后兼容
                System.Diagnostics.Debug.WriteLine($"Failed to set Schema: {ex.Message}");
            }
        }

        return metadata;
    }

    /// <summary>
    /// 安全地设置对象属性
    /// </summary>
    private static void SetPropertySafely(object target, string propertyName, object? value)
    {
        try
        {
            var property = target.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite && value != null)
            {
                // 特殊处理Schema属性（需要KernelJsonSchema类型）
                if (propertyName == "Schema" && property.PropertyType.Name == "KernelJsonSchema")
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(value);
                    
                    var kernelJsonSchemaType = property.PropertyType;
                    var ctor = kernelJsonSchemaType.GetConstructor(new Type[] { typeof(string) });
                    if (ctor != null)
                    {
                        var kernelJsonSchema = ctor.Invoke(new object[] { jsonString });
                        property.SetValue(target, kernelJsonSchema);
                    }
                }
                else
                {
                    property.SetValue(target, value);
                }
            }
        }
        catch
        {
            // 忽略设置失败的情况，保持向后兼容性
        }
    }

    /// <summary>
    /// 生成完整的JsonSchema对象
    /// </summary>
    private object GenerateJsonSchema()
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = Type ?? "string"
        };

        if (!string.IsNullOrEmpty(Description))
            schema["description"] = Description;

        // 对于数组类型，必须包含items属性
        if (Type == "array")
        {
            if (ArrayItems != null)
            {
                schema["items"] = ArrayItems.GenerateJsonSchema();
            }
            else
            {
                // 默认items为object类型
                schema["items"] = new Dictionary<string, object> { ["type"] = "object" };
            }
        }

        // 对于对象类型
        if (Type == "object" && ObjectProperties != null)
        {
            var properties = new Dictionary<string, object>();
            foreach (var (propName, propInfo) in ObjectProperties)
            {
                properties[propName] = propInfo.GenerateJsonSchema();
            }
            schema["properties"] = properties;

            if (RequiredProperties?.Any() == true)
            {
                schema["required"] = RequiredProperties;
            }

            if (AdditionalProperties.HasValue)
            {
                schema["additionalProperties"] = AdditionalProperties.Value;
            }
        }

        // 添加约束
        if (EnumValues?.Any() == true)
            schema["enum"] = EnumValues;

        if (MinLength.HasValue)
            schema["minLength"] = MinLength.Value;

        if (MaxLength.HasValue)
            schema["maxLength"] = MaxLength.Value;

        if (Minimum.HasValue)
            schema["minimum"] = Minimum.Value;

        if (Maximum.HasValue)
            schema["maximum"] = Maximum.Value;

        if (!string.IsNullOrEmpty(Pattern))
            schema["pattern"] = Pattern;

        if (!string.IsNullOrEmpty(Format))
            schema["format"] = Format;

        if (DefaultValue != null)
            schema["default"] = DefaultValue;

        return schema;
    }

    /// <summary>
    /// 获取对应的.NET类型
    /// </summary>
    /// <returns>.NET类型</returns>
    public System.Type GetDotNetType()
    {
        return Type switch
        {
            "string" => typeof(string),
            "integer" => typeof(int),
            "number" => typeof(double),
            "boolean" => typeof(bool),
            "array" => typeof(object[]),
            "object" => typeof(object),
            _ => typeof(object)
        };
    }

    /// <summary>
    /// 生成用于Semantic Kernel的参数描述
    /// </summary>
    /// <returns>增强的参数描述</returns>
    public string GetEnhancedDescription()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(Description))
        {
            parts.Add(Description);
        }

        // 对于数组类型，添加完整的JSON Schema信息
        if (Type == "array")
        {
            var schema = GenerateJsonSchema();
            var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = false 
            });
            parts.Add($"JSON Schema: {schemaJson}");
        }
        else
        {
            // 添加类型信息
            if (!string.IsNullOrEmpty(Type))
            {
                parts.Add($"Type: {Type}");
            }
        }

        // 添加约束信息
        if (EnumValues?.Any() == true)
        {
            parts.Add($"Allowed values: {string.Join(", ", EnumValues)}");
        }

        if (MinLength.HasValue || MaxLength.HasValue)
        {
            var lengthInfo = $"Length: {MinLength ?? 0}-{MaxLength?.ToString() ?? "unlimited"}";
            parts.Add(lengthInfo);
        }

        if (Minimum.HasValue || Maximum.HasValue)
        {
            var rangeInfo = $"Range: {Minimum?.ToString() ?? "unlimited"}-{Maximum?.ToString() ?? "unlimited"}";
            parts.Add(rangeInfo);
        }

        if (!string.IsNullOrEmpty(Format))
        {
            parts.Add($"Format: {Format}");
        }

        if (!string.IsNullOrEmpty(Pattern))
        {
            parts.Add($"Pattern: {Pattern}");
        }

        return string.Join(". ", parts);
    }
    #endregion

    #region 辅助方法
    private static string GetPrimaryType(JsonElement typeElement)
    {
        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() ?? "any";
        }
        else if (typeElement.ValueKind == JsonValueKind.Array)
        {
            // 对于联合类型，选择第一个非null类型
            foreach (var item in typeElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var typeStr = item.GetString();
                    if (typeStr != null && typeStr != "null")
                    {
                        return typeStr;
                    }
                }
            }
        }
        return "any";
    }

    private static List<string>? GetTypeArray(JsonElement typeElement)
    {
        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            return typeElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToList();
        }
        return null;
    }

    private static object? ConvertJsonElementToBasicType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
    #endregion
}
