using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.TypeTestAgent.Options;
using Aevatar.GAgents.TypeTestAgent.State;
using Orleans;

namespace Aevatar.GAgents.TypeTestAgent.Agent;

/// <summary>
/// TypeTestAgent接口定义
/// </summary>
public interface ITypeTestAgent : IAIGAgent, IGrain
{
    /// <summary>
    /// 执行指定类型的数据类型测试
    /// </summary>
    /// <param name="testType">测试类型</param>
    /// <returns>测试结果</returns>
    Task<string> ExecuteTypeTestAsync(string testType);
    
    /// <summary>
    /// 获取当前配置信息
    /// </summary>
    /// <returns>配置信息</returns>
    Task<TypeTestConfigDto> GetConfigurationAsync();
    
    /// <summary>
    /// 验证所有数据类型配置
    /// </summary>
    /// <returns>验证结果</returns>
    Task<Dictionary<string, object>> ValidateAllTypesAsync();
    
    /// <summary>
    /// 获取测试执行统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    Task<Dictionary<string, int>> GetTestStatisticsAsync();
    
    /// <summary>
    /// 重置测试状态
    /// </summary>
    /// <returns>操作结果</returns>
    Task<bool> ResetTestStateAsync();
    
    /// <summary>
    /// 执行AI驱动的类型分析
    /// </summary>
    /// <param name="input">输入内容</param>
    /// <returns>AI分析结果</returns>
    Task<string> AnalyzeTypeAsync(string input);
} 