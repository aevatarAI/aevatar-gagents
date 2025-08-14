using System;
using System.Linq;

namespace Aevatar.GAgents.Basic;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class GenericMetaAttribute : Attribute
{
    public string? Category { get; set; }
    public string Key { get; set; }
    public object Value { get; set; }
    
    // 多层路径支持 - 存储完整的路径层级
    public string[]? PathLevels { get; set; }

    // 两参数构造函数：[GenericMeta("key", value)] - 直接属性，无层级
    public GenericMetaAttribute(string key, object value)
    {
        Category = null;
        Key = key;
        Value = value;
        PathLevels = null;
    }

    // 三参数构造函数：[GenericMeta("config", "key", value)] - 单层分类
    public GenericMetaAttribute(string category, string key, object value)
    {
        Category = category;
        Key = key;
        Value = value;
        PathLevels = new[] { category };
    }

    // 多层路径构造函数：[GenericMeta("config", "api", "timeout", value)] - 任意多层
    // 用法：[GenericMeta("level1", "level2", "level3", "key", value)]
    public GenericMetaAttribute(params object[] pathAndValue)
    {
        if (pathAndValue.Length < 2)
            throw new ArgumentException("至少需要key和value两个参数");

        // 最后一个参数是value
        Value = pathAndValue[pathAndValue.Length - 1];
        
        // 倒数第二个参数是key
        Key = pathAndValue[pathAndValue.Length - 2].ToString()!;
        
        // 前面的所有参数都是路径层级
        if (pathAndValue.Length > 2)
        {
            PathLevels = pathAndValue.Take(pathAndValue.Length - 2)
                                   .Select(p => p.ToString()!)
                                   .ToArray();
            Category = PathLevels[0]; // 保持兼容性
        }
        else
        {
            PathLevels = null;
            Category = null;
        }
    }
}