using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// Agent信息智能提取器，自动从现有代码推断Agent信息
/// </summary>
public class AgentInfoExtractor
{
    /// <summary>
    /// 从Agent类型自动提取信息，支持fallback到推断逻辑
    /// </summary>
    public static AgentIndexInfo ExtractAgentInfo(Type agentType)
    {
        var info = new AgentIndexInfo
        {
            Id = agentType.Name,
            AgentType = agentType.FullName ?? agentType.Name,
            InterfaceType = GetPrimaryInterface(agentType)
        };

        // 优先使用手动标记的AgentDescription
        var agentDesc = agentType.GetCustomAttribute<AgentDescriptionAttribute>();
        if (agentDesc != null)
        {
            info.Name = agentDesc.Name;
            info.L1Description = agentDesc.L1Description;
            info.L2Description = agentDesc.L2Description;
            info.Category = agentDesc.Category;
            info.Capabilities = agentDesc.Capabilities.ToList();
            info.Tags = agentDesc.Tags.ToList();
            info.InputFormat = agentDesc.InputFormat;
            info.OutputFormat = agentDesc.OutputFormat;
            info.UsageExample = agentDesc.UsageExample;
            return info;
        }

        // Fallback：自动推断
        return InferAgentInfo(agentType, info);
    }

    /// <summary>
    /// 自动推断Agent信息
    /// </summary>
    private static AgentIndexInfo InferAgentInfo(Type agentType, AgentIndexInfo info)
    {
        // 1. 从类名推断名称和分类
        info.Name = InferNameFromType(agentType);
        info.Category = InferCategoryFromType(agentType);

        // 2. 从现有Description属性提取
        var descAttr = agentType.GetCustomAttribute<DescriptionAttribute>();
        if (descAttr != null)
        {
            info.L1Description = descAttr.Description;
            info.L2Description = $"Agent {info.Name}: {descAttr.Description}";
        }

        // 3. 从XML注释提取（如果可用）
        ExtractFromXmlDocumentation(agentType, info);

        // 4. 从接口和基类推断能力
        info.Capabilities = InferCapabilities(agentType);
        info.Tags = InferTags(agentType);

        // 5. 设置默认值
        if (string.IsNullOrEmpty(info.L1Description))
        {
            info.L1Description = $"基于{info.Category}的Agent，提供{string.Join("、", info.Capabilities)}能力";
        }

        if (string.IsNullOrEmpty(info.L2Description))
        {
            info.L2Description = $"这是一个{info.Category}类型的Agent，实现了{string.Join("、", info.Capabilities)}等功能。适用于需要{info.Category}能力的场景。";
        }

        info.InputFormat = "text";
        info.OutputFormat = "text";
        info.UsageExample = $"// 使用{info.Name}\nvar result = await agent.ProcessAsync(input);";

        return info;
    }

    /// <summary>
    /// 从类型名称推断Agent名称
    /// </summary>
    private static string InferNameFromType(Type type)
    {
        var name = type.Name;
        
        // 移除常见后缀
        var suffixes = new[] { "GAgent", "Agent", "Grain" };
        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix))
            {
                name = name.Substring(0, name.Length - suffix.Length);
                break;
            }
        }

        // 转换为友好名称
        return Regex.Replace(name, "([A-Z])", " $1").Trim();
    }

    /// <summary>
    /// 从类型推断分类
    /// </summary>
    private static string InferCategoryFromType(Type type)
    {
        var typeName = type.Name.ToLower();
        var namespaceName = type.Namespace?.ToLower() ?? "";

        if (typeName.Contains("ai") || typeName.Contains("chat") || typeName.Contains("llm"))
            return "AI";
        if (typeName.Contains("social") || namespaceName.Contains("social"))
            return "Social";
        if (typeName.Contains("twitter") || typeName.Contains("telegram"))
            return "Social";
        if (typeName.Contains("workflow") || typeName.Contains("router"))
            return "Workflow";
        if (typeName.Contains("blockchain") || typeName.Contains("aelf"))
            return "Blockchain";
        if (typeName.Contains("group") || typeName.Contains("multi"))
            return "Collaboration";
        if (typeName.Contains("retrieval") || typeName.Contains("graph"))
            return "Knowledge";

        return "General";
    }

    /// <summary>
    /// 推断Agent能力
    /// </summary>
    private static List<string> InferCapabilities(Type type)
    {
        var capabilities = new List<string>();
        var typeName = type.Name.ToLower();
        var interfaces = type.GetInterfaces().Select(i => i.Name.ToLower()).ToList();

        // 从类型名称推断
        if (typeName.Contains("chat")) capabilities.Add("chat");
        if (typeName.Contains("ai")) capabilities.Add("ai-processing");
        if (typeName.Contains("social")) capabilities.Add("social-interaction");
        if (typeName.Contains("workflow")) capabilities.Add("workflow-management");
        if (typeName.Contains("router")) capabilities.Add("routing");
        if (typeName.Contains("retrieval")) capabilities.Add("information-retrieval");
        if (typeName.Contains("multi")) capabilities.Add("multi-agent-coordination");

        // 从接口推断
        if (interfaces.Any(i => i.Contains("chat"))) capabilities.Add("conversation");
        if (interfaces.Any(i => i.Contains("ai"))) capabilities.Add("ai-capabilities");

        // 默认能力
        if (!capabilities.Any())
        {
            capabilities.Add("task-processing");
        }

        return capabilities.Distinct().ToList();
    }

    /// <summary>
    /// 推断标签
    /// </summary>
    private static List<string> InferTags(Type type)
    {
        var tags = new List<string>();
        var category = InferCategoryFromType(type);
        
        tags.Add(category.ToLower());
        tags.Add("agent");
        
        if (type.Name.Contains("AI")) tags.Add("ai");
        if (type.Name.Contains("Chat")) tags.Add("chat");
        if (type.Name.Contains("Social")) tags.Add("social");

        return tags.Distinct().ToList();
    }

    /// <summary>
    /// 获取主要接口
    /// </summary>
    private static string GetPrimaryInterface(Type type)
    {
        var interfaces = type.GetInterfaces()
            .Where(i => !i.IsGenericType && !i.Name.StartsWith("I") || 
                       i.Name.StartsWith("I") && i.Name.Contains("Agent"))
            .ToList();

        return interfaces.FirstOrDefault()?.FullName ?? "";
    }

    /// <summary>
    /// 从XML文档注释提取信息（简化版本）
    /// </summary>
    private static void ExtractFromXmlDocumentation(Type type, AgentIndexInfo info)
    {
        // 这里可以扩展XML文档解析逻辑
        // 目前为简化实现
    }

    /// <summary>
    /// 批量扫描程序集中的所有Agent
    /// </summary>
    public static List<AgentIndexInfo> ScanAssemblyForAgents(Assembly assembly)
    {
        var agents = new List<AgentIndexInfo>();
        
        try
        {
            var agentTypes = assembly.GetTypes()
                .Where(t => IsAgentType(t))
                .ToList();

            foreach (var type in agentTypes)
            {
                try
                {
                    var agentInfo = ExtractAgentInfo(type);
                    agents.Add(agentInfo);
                }
                catch (Exception ex)
                {
                    // 记录但不中断整个扫描过程
                    Console.WriteLine($"Failed to extract info for {type.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to scan assembly {assembly.FullName}: {ex.Message}");
        }

        return agents;
    }

    /// <summary>
    /// 判断类型是否为Agent
    /// </summary>
    public static bool IsAgentType(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
            return false;

        // 有AgentDescription标记
        if (type.GetCustomAttribute<AgentDescriptionAttribute>() != null)
            return true;

        // 类名包含Agent
        if (type.Name.EndsWith("Agent") || type.Name.EndsWith("GAgent"))
            return true;

        // 实现了Agent相关接口
        var interfaces = type.GetInterfaces();
        if (interfaces.Any(i => i.Name.Contains("Agent") || i.Name.Contains("GAgent")))
            return true;

        return false;
    }
} 