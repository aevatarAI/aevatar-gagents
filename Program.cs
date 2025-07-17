using System;
using System.IO;
using System.Reflection;
using Aevatar.GAgents.AI.Common;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Agent扫描器测试 ===");
        Console.WriteLine("正在验证Agent扫描功能...\n");

        // 测试基本功能
        TestBasicFunctionality();

        // 测试SocialGAgent扫描
        TestSocialGAgentScanning();

        Console.WriteLine("\n测试完成！");
    }

    static void TestBasicFunctionality()
    {
        Console.WriteLine("1. 验证核心类型...");
        try
        {
            var attrType = typeof(AgentDescriptionAttribute);
            var infoType = typeof(AgentIndexInfo);
            var scannerType = typeof(SimpleAgentScanner);
            
            Console.WriteLine($"✓ {attrType.Name} - OK");
            Console.WriteLine($"✓ {infoType.Name} - OK");
            Console.WriteLine($"✓ {scannerType.Name} - OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 基本功能测试失败: {ex.Message}");
        }
    }

    static void TestSocialGAgentScanning()
    {
        Console.WriteLine("\n2. 扫描SocialGAgent程序集...");
        
        var assemblyPath = "../src/Aevatar.GAgents.SocialGAgent/bin/Debug/net9.0/Aevatar.GAgents.SocialGAgent.dll";
        
        if (!File.Exists(assemblyPath))
        {
            Console.WriteLine($"❌ 程序集不存在: {assemblyPath}");
            return;
        }
        
        Console.WriteLine($"✓ 找到程序集: {Path.GetFileName(assemblyPath)}");
        
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            Console.WriteLine($"✓ 成功加载程序集");
            
            var agents = SimpleAgentScanner.ScanAgentsInAssembly(assembly);
            Console.WriteLine($"✓ 扫描完成，发现 {agents.Count} 个Agent");
            
            foreach (var agent in agents)
            {
                Console.WriteLine($"\n📋 Agent详情:");
                Console.WriteLine($"   名称: {agent.Name}");
                Console.WriteLine($"   ID: {agent.Id}");
                Console.WriteLine($"   分类: {agent.Category}");
                Console.WriteLine($"   描述: {agent.L1Description}");
                if (agent.Capabilities.Count > 0)
                    Console.WriteLine($"   能力: {string.Join(", ", agent.Capabilities)}");
                if (agent.Tags.Count > 0)
                    Console.WriteLine($"   标签: {string.Join(", ", agent.Tags)}");
            }
            
            if (agents.Count == 0)
            {
                Console.WriteLine("⚠️  没有发现标记的Agent");
                Console.WriteLine("   请确认SocialGAgent类上有AgentDescription标记");
            }
            else
            {
                Console.WriteLine($"\n🎉 测试成功! 发现 {agents.Count} 个Agent");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 扫描失败: {ex.Message}");
            Console.WriteLine($"   详细信息: {ex.GetType().Name}");
        }
    }
} 