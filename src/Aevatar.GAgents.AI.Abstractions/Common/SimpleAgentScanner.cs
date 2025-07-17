using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// 简单的Agent扫描器，只处理手动标记的Agent
/// </summary>
public class SimpleAgentScanner
{
    /// <summary>
    /// 扫描程序集中所有带有AgentDescription标记的Agent
    /// </summary>
    public static List<AgentIndexInfo> ScanAgentsInAssembly(Assembly assembly)
    {
        var agents = new List<AgentIndexInfo>();
        
        try
        {
            var types = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<AgentDescriptionAttribute>() != null)
                .ToList();

            foreach (var type in types)
            {
                var agentInfo = ExtractAgentInfo(type);
                if (agentInfo != null)
                {
                    agents.Add(agentInfo);
                }
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不中断
            Console.WriteLine($"Error scanning assembly {assembly.FullName}: {ex.Message}");
        }

        return agents;
    }

    /// <summary>
    /// 从类型提取Agent信息
    /// </summary>
    private static AgentIndexInfo? ExtractAgentInfo(Type type)
    {
        var attr = type.GetCustomAttribute<AgentDescriptionAttribute>();
        if (attr == null) return null;

        return new AgentIndexInfo
        {
            Id = type.Name,
            Name = attr.Name,
            Category = attr.Category,
            L1Description = attr.L1Description,
            L2Description = attr.L2Description,
            Capabilities = attr.Capabilities?.ToList() ?? new List<string>(),
            Tags = attr.Tags?.ToList() ?? new List<string>(),
            InputFormat = attr.InputFormat,
            OutputFormat = attr.OutputFormat,
            UsageExample = attr.UsageExample,
            AgentType = type.FullName ?? type.Name,
            InterfaceType = GetMainInterface(type)
        };
    }

    /// <summary>
    /// 获取主要接口
    /// </summary>
    private static string GetMainInterface(Type type)
    {
        var interfaces = type.GetInterfaces()
            .Where(i => i.Name.Contains("Agent"))
            .ToList();
        
        return interfaces.FirstOrDefault()?.FullName ?? "";
    }

    /// <summary>
    /// 扫描当前应用域中的所有程序集
    /// </summary>
    public static List<AgentIndexInfo> ScanAllLoadedAssemblies()
    {
        var allAgents = new List<AgentIndexInfo>();
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.Contains("GAgent") == true)
            .ToList();

        foreach (var assembly in assemblies)
        {
            var agents = ScanAgentsInAssembly(assembly);
            allAgents.AddRange(agents);
        }

        return allAgents;
    }
} 