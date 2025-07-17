using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// Agent描述自动生成工具，帮助开发者快速为现有Agent添加标记
/// </summary>
public class AgentDescriptionGenerator
{
    /// <summary>
    /// 为指定程序集中的所有Agent生成AgentDescription标记代码
    /// </summary>
    public static string GenerateDescriptionsForAssembly(Assembly assembly)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// 自动生成的Agent描述标记");
        sb.AppendLine("// 请复制到对应的Agent类上方，并根据实际情况调整");
        sb.AppendLine();

        var agentTypes = assembly.GetTypes()
            .Where(t => AgentInfoExtractor.IsAgentType(t))
            .Where(t => t.GetCustomAttribute<AgentDescriptionAttribute>() == null) // 只处理未标记的
            .ToList();

        foreach (var agentType in agentTypes)
        {
            var agentInfo = AgentInfoExtractor.ExtractAgentInfo(agentType);
            var code = GenerateAttributeCode(agentInfo);
            
            sb.AppendLine($"// 对于类: {agentType.FullName}");
            sb.AppendLine(code);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// 为单个Agent类型生成AgentDescription标记代码
    /// </summary>
    public static string GenerateDescriptionForType(Type agentType)
    {
        var agentInfo = AgentInfoExtractor.ExtractAgentInfo(agentType);
        return GenerateAttributeCode(agentInfo);
    }

    /// <summary>
    /// 生成AgentDescription属性代码
    /// </summary>
    private static string GenerateAttributeCode(AgentIndexInfo agentInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[AgentDescription(");
        sb.AppendLine($"    Name = \"{EscapeString(agentInfo.Name)}\",");
        sb.AppendLine($"    L1Description = \"{EscapeString(agentInfo.L1Description)}\",");
        sb.AppendLine($"    L2Description = \"{EscapeString(agentInfo.L2Description)}\",");
        sb.AppendLine($"    Category = \"{agentInfo.Category}\",");
        
        if (agentInfo.Capabilities.Any())
        {
            var capabilities = string.Join("\", \"", agentInfo.Capabilities);
            sb.AppendLine($"    Capabilities = new[] {{ \"{capabilities}\" }},");
        }
        
        if (agentInfo.Tags.Any())
        {
            var tags = string.Join("\", \"", agentInfo.Tags);
            sb.AppendLine($"    Tags = new[] {{ \"{tags}\" }},");
        }
        
        sb.AppendLine($"    InputFormat = \"{agentInfo.InputFormat}\",");
        sb.AppendLine($"    OutputFormat = \"{agentInfo.OutputFormat}\",");
        sb.AppendLine($"    UsageExample = \"{EscapeString(agentInfo.UsageExample)}\"");
        sb.AppendLine(")]");
        
        return sb.ToString();
    }

    /// <summary>
    /// 转义字符串中的特殊字符
    /// </summary>
    private static string EscapeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        return input.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    /// <summary>
    /// 生成Agent信息概览报告
    /// </summary>
    public static string GenerateAgentReport(Assembly assembly)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Agent信息概览报告");
        sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"程序集: {assembly.GetName().Name}");
        sb.AppendLine();

        var allAgents = AgentInfoExtractor.ScanAssemblyForAgents(assembly);
        var markedAgents = allAgents.Where(a => HasManualDescription(assembly, a.AgentType)).ToList();
        var autoInferredAgents = allAgents.Except(markedAgents).ToList();

        sb.AppendLine($"## 统计信息");
        sb.AppendLine($"- 总Agent数量: {allAgents.Count}");
        sb.AppendLine($"- 已手动标记: {markedAgents.Count}");
        sb.AppendLine($"- 自动推断: {autoInferredAgents.Count}");
        sb.AppendLine($"- 标记完成度: {(markedAgents.Count * 100.0 / allAgents.Count):F1}%");
        sb.AppendLine();

        // 按分类统计
        var categories = allAgents.GroupBy(a => a.Category).ToList();
        sb.AppendLine("## 按分类统计");
        foreach (var category in categories)
        {
            sb.AppendLine($"- {category.Key}: {category.Count()}个");
        }
        sb.AppendLine();

        // 需要手动标记的Agent列表
        if (autoInferredAgents.Any())
        {
            sb.AppendLine("## 需要手动标记的Agent");
            sb.AppendLine("以下Agent当前使用自动推断信息，建议添加手动标记以提供更准确的描述：");
            sb.AppendLine();
            
            foreach (var agent in autoInferredAgents)
            {
                sb.AppendLine($"### {agent.Name} (`{agent.AgentType}`)");
                sb.AppendLine($"- **分类**: {agent.Category}");
                sb.AppendLine($"- **推断描述**: {agent.L1Description}");
                sb.AppendLine($"- **推断能力**: {string.Join(", ", agent.Capabilities)}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 检查Agent是否有手动标记
    /// </summary>
    private static bool HasManualDescription(Assembly assembly, string agentTypeName)
    {
        var type = assembly.GetTypes().FirstOrDefault(t => t.FullName == agentTypeName);
        return type?.GetCustomAttribute<AgentDescriptionAttribute>() != null;
    }

    /// <summary>
    /// 将生成的代码保存到文件
    /// </summary>
    public static void SaveGeneratedCode(string content, string filePath)
    {
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// 批量处理项目中的所有程序集
    /// </summary>
    public static Dictionary<string, string> GenerateForAllAssemblies(string assemblyPattern = "*GAgent*.dll")
    {
        var results = new Dictionary<string, string>();
        var assemblies = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, assemblyPattern)
            .Select(Assembly.LoadFrom)
            .ToList();

        foreach (var assembly in assemblies)
        {
            try
            {
                var code = GenerateDescriptionsForAssembly(assembly);
                if (!string.IsNullOrWhiteSpace(code))
                {
                    results[assembly.GetName().Name!] = code;
                }
            }
            catch (Exception ex)
            {
                results[assembly.GetName().Name!] = $"// 生成失败: {ex.Message}";
            }
        }

        return results;
    }
} 