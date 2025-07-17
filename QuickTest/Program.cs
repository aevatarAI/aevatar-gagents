using System;
using System.IO;
using System.Reflection;

Console.WriteLine("=== Agent扫描测试 ===\n");

// 测试两个Agent程序集  
TestAssembly("../src/Aevatar.GAgents.SocialGAgent/bin/Debug/net9.0/Aevatar.GAgents.SocialGAgent.dll", "SocialGAgent");
TestAssembly("../src/Aevatar.GAgents.Router/bin/Debug/net9.0/Aevatar.GAgents.Router.dll", "RouterGAgent");

Console.WriteLine("\n测试完成！");

static void TestAssembly(string assemblyPath, string expectedAgentName)
{
    Console.WriteLine($"测试程序集: {Path.GetFileName(assemblyPath)}");
    
    if (!File.Exists(assemblyPath))
    {
        Console.WriteLine($"❌ 程序集不存在: {assemblyPath}");
        return;
    }
    
    try
    {
        // 加载程序集
        var assembly = Assembly.LoadFrom(assemblyPath);
        Console.WriteLine($"✓ 成功加载程序集");
        
        // 扫描AgentDescription标记的类
        int agentCount = 0;
        foreach (var type in assembly.GetTypes())
        {
            // 查找AgentDescription标记
            var attrs = type.GetCustomAttributes(false);
            foreach (var attr in attrs)
            {
                if (attr.GetType().Name == "AgentDescriptionAttribute")
                {
                    agentCount++;
                    Console.WriteLine($"✓ 发现Agent: {type.Name}");
                    
                    // 使用反射读取属性值
                    var nameProperty = attr.GetType().GetProperty("Name");
                    var l1Property = attr.GetType().GetProperty("L1Description");
                    var categoryProperty = attr.GetType().GetProperty("Category");
                    
                    if (nameProperty != null)
                        Console.WriteLine($"  名称: {nameProperty.GetValue(attr)}");
                    if (l1Property != null)
                        Console.WriteLine($"  描述: {l1Property.GetValue(attr)}");
                    if (categoryProperty != null)
                        Console.WriteLine($"  分类: {categoryProperty.GetValue(attr)}");
                    break;
                }
            }
        }
        
        if (agentCount > 0)
        {
            Console.WriteLine($"🎉 成功！在{expectedAgentName}中发现 {agentCount} 个Agent");
        }
        else
        {
            Console.WriteLine($"⚠️  在{expectedAgentName}中没有发现AgentDescription标记的Agent");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 测试失败: {ex.Message}");
    }
    
    Console.WriteLine();
}
