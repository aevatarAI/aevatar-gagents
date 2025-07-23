using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.TypeTestAgent.Events;
using Aevatar.GAgents.TypeTestAgent.Options;
using Aevatar.GAgents.TypeTestAgent.State;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;

namespace Aevatar.GAgents.TypeTestAgent.Agent;

[Description("Data type testing and validation agent")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(TypeTestAgent))]
public class TypeTestAgent : AIGAgentBase<TypeTestAgentState, TypeTestEvent, EventBase, TypeTestConfigDto>,
    ITypeTestAgent
{
    private readonly ILogger<TypeTestAgent> _logger;

    public TypeTestAgent(ILogger<TypeTestAgent> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 实现新的JSON序列化描述方案
    /// </summary>
    public override Task<string> GetDescriptionAsync()
    {
        var descriptionInfo = new AgentDescriptionInfo
        {
            Id = "TypeTestAgent",
            Name = "Type Testing & Validation Agent",
            L1Description = "Comprehensive data type testing agent supporting 15+ data types with AI-powered validation and analysis capabilities",
            L2Description = "Advanced testing agent designed for validating and analyzing various data types including primitives, complex objects, and custom types. Features AI-driven type inference, validation rules, configuration management, and detailed testing reports with C# native default value support.",
            Category = "Testing",
            Capabilities = new List<string> 
            { 
                "type-validation", 
                "data-analysis", 
                "ai-inference", 
                "configuration-testing", 
                "statistics-reporting",
                "primitive-types",
                "complex-types",
                "enum-validation",
                "datetime-handling",
                "numeric-precision"
            },
            Tags = new List<string> 
            { 
                "testing", 
                "validation", 
                "data-types", 
                "ai-analysis", 
                "configuration",
                "csharp",
                "primitives",
                "statistics"
            }
        };
        return Task.FromResult(JsonConvert.SerializeObject(descriptionInfo));
    }

    /// <summary>
    /// 执行指定类型的数据类型测试
    /// </summary>
    public async Task<string> ExecuteTypeTestAsync(string testType)
    {
        _logger.LogInformation("ExecuteTypeTestAsync for type: {TestType}", testType);
        
        // 记录测试执行事件
        RaiseEvent(new TestExecutionSEvent
        {
            ExecutionTime = DateTime.UtcNow,
            TestType = testType,
            IsSuccess = true,
            Result = $"Test executed for {testType}"
        });
        await ConfirmEvents();

        // 根据测试类型执行不同的验证逻辑
        var result = testType.ToLowerInvariant() switch
        {
            "string" => ValidateStringType(),
            "int" => ValidateIntType(),
            "bool" => ValidateBoolType(),
            "double" => ValidateDoubleType(),
            "decimal" => ValidateDecimalType(),
            "datetime" => ValidateDateTimeType(),
            "timespan" => ValidateTimeSpanType(),
            "enum" => ValidateEnumType(),
            "guid" => ValidateGuidType(),
            "long" => ValidateLongType(),
            "float" => ValidateFloatType(),
            "ushort" => ValidateUShortType(),
            "byte" => ValidateByteType(),
            "short" => ValidateShortType(),
            "all" => ValidateAllTypes(),
            _ => $"Unknown test type: {testType}"
        };

        // 使用AI分析测试结果
        if (!State.PromptTemplate.IsNullOrEmpty())
        {
            var aiAnalysis = await ChatWithHistory($"Analyze this test result: {result}");
            var aiContent = aiAnalysis?.FirstOrDefault()?.Content ?? "AI analysis not available";
            result += $"\n\nAI Analysis: {aiContent}";
        }

        return result;
    }

    /// <summary>
    /// 获取当前配置信息
    /// </summary>
    public Task<TypeTestConfigDto> GetConfigurationAsync()
    {
        return Task.FromResult(new TypeTestConfigDto
        {
            Name = State.Name,
            MaxCount = State.MaxCount,
            IsEnabled = State.IsEnabled,
            Precision = State.Precision,
            Price = State.Price,
            StartDate = State.StartDate,
            Duration = State.Duration,
            Mode = State.Mode,
            SessionId = State.SessionId,
            MaxFileSize = State.MaxFileSize,
            Threshold = State.Threshold,
            ApiEndpoint = State.ApiEndpoint,
            Port = State.Port,
            RetryCount = State.RetryCount,
            ConfigVersion = State.ConfigVersion
        });
    }

    /// <summary>
    /// 验证所有数据类型配置
    /// </summary>
    public Task<Dictionary<string, object>> ValidateAllTypesAsync()
    {
        var results = new Dictionary<string, object>
        {
            ["String"] = new { Value = State.Name, Type = "System.String", IsValid = !string.IsNullOrEmpty(State.Name) },
            ["Int32"] = new { Value = State.MaxCount, Type = "System.Int32", IsValid = State.MaxCount > 0 },
            ["Boolean"] = new { Value = State.IsEnabled, Type = "System.Boolean", IsValid = true },
            ["Double"] = new { Value = State.Precision, Type = "System.Double", IsValid = State.Precision > 0 },
            ["Decimal"] = new { Value = State.Price, Type = "System.Decimal", IsValid = State.Price >= 0 },
            ["DateTime"] = new { Value = State.StartDate, Type = "System.DateTime", IsValid = State.StartDate != default },
            ["TimeSpan"] = new { Value = State.Duration, Type = "System.TimeSpan", IsValid = State.Duration.TotalSeconds > 0 },
            ["Enum"] = new { Value = State.Mode, Type = "TestMode", IsValid = Enum.IsDefined(typeof(TestMode), State.Mode) },
            ["Guid"] = new { Value = State.SessionId, Type = "System.Guid", IsValid = State.SessionId != Guid.Empty },
            ["Int64"] = new { Value = State.MaxFileSize, Type = "System.Int64", IsValid = State.MaxFileSize > 0 },
            ["Single"] = new { Value = State.Threshold, Type = "System.Single", IsValid = State.Threshold is >= 0 and <= 1 },
            ["UInt16"] = new { Value = State.Port, Type = "System.UInt16", IsValid = State.Port > 0 },
            ["Byte"] = new { Value = State.RetryCount, Type = "System.Byte", IsValid = State.RetryCount > 0 },
            ["Int16"] = new { Value = State.ConfigVersion, Type = "System.Int16", IsValid = State.ConfigVersion > 0 }
        };

        return Task.FromResult(results);
    }

    /// <summary>
    /// 获取测试执行统计信息
    /// </summary>
    public Task<Dictionary<string, int>> GetTestStatisticsAsync()
    {
        return Task.FromResult(new Dictionary<string, int>
        {
            ["TotalExecutions"] = State.TestExecutionCount,
            ["ConfiguredTypes"] = 15,
            ["LastExecutionDay"] = State.LastTestTime != default ? State.LastTestTime.Day : 0,
            ["ConfigVersion"] = State.ConfigVersion
        });
    }

    /// <summary>
    /// 重置测试状态
    /// </summary>
    public async Task<bool> ResetTestStateAsync()
    {
        RaiseEvent(new ConfigurationStatusSEvent
        {
            IsConfigured = false,
            ConfiguredTime = DateTime.UtcNow
        });
        await ConfirmEvents();
        
        _logger.LogInformation("Test state has been reset");
        return true;
    }

    /// <summary>
    /// 执行AI驱动的类型分析
    /// </summary>
    public async Task<string> AnalyzeTypeAsync(string input)
    {
        if (State.PromptTemplate.IsNullOrEmpty())
        {
            return "AI analysis not available - agent not initialized with LLM configuration";
        }

        var prompt = $"Analyze the following input and determine its data type, provide validation suggestions, and recommend the best C# type to represent it: {input}";
        
        var aiResponse = await ChatWithHistory(prompt);
        var analysis = aiResponse?.FirstOrDefault()?.Content ?? "AI analysis failed";
        
        // 记录AI分析执行
        RaiseEvent(new TestExecutionSEvent
        {
            ExecutionTime = DateTime.UtcNow,
            TestType = "AI-Analysis",
            IsSuccess = !string.IsNullOrEmpty(analysis),
            Result = $"Analyzed input: {input.Take(50)}..."
        });
        await ConfirmEvents();

        return analysis;
    }

    /// <summary>
    /// 配置Agent
    /// </summary>
    protected override async Task PerformConfigAsync(TypeTestConfigDto config)
    {
        _logger.LogDebug("PerformConfigAsync with data: {Config}", JsonConvert.SerializeObject(config));
        
        RaiseEvent(new TypeTestConfigSEvent
        {
            Name = config.Name,
            MaxCount = config.MaxCount,
            IsEnabled = config.IsEnabled,
            Precision = config.Precision,
            Price = config.Price,
            StartDate = config.StartDate,
            Duration = config.Duration,
            Mode = config.Mode,
            SessionId = config.SessionId,
            MaxFileSize = config.MaxFileSize,
            Threshold = config.Threshold,
            ApiEndpoint = config.ApiEndpoint,
            Port = config.Port,
            RetryCount = config.RetryCount,
            ConfigVersion = config.ConfigVersion
        });

        RaiseEvent(new ConfigurationStatusSEvent
        {
            IsConfigured = true,
            ConfiguredTime = DateTime.UtcNow
        });

        await ConfirmEvents();
    }

    /// <summary>
    /// 状态转换处理
    /// </summary>
    protected override void AIGAgentTransitionState(TypeTestAgentState state, StateLogEventBase<TypeTestEvent> @event)
    {
        _logger.LogDebug("AIGAgentTransitionState: {Event}", JsonConvert.SerializeObject(@event));

        switch (@event)
        {
            case TypeTestConfigSEvent configEvent:
                state.Name = configEvent.Name;
                state.MaxCount = configEvent.MaxCount;
                state.IsEnabled = configEvent.IsEnabled;
                state.Precision = configEvent.Precision;
                state.Price = configEvent.Price;
                state.StartDate = configEvent.StartDate;
                state.Duration = configEvent.Duration;
                state.Mode = configEvent.Mode;
                state.SessionId = configEvent.SessionId;
                state.MaxFileSize = configEvent.MaxFileSize;
                state.Threshold = configEvent.Threshold;
                state.ApiEndpoint = configEvent.ApiEndpoint;
                state.Port = configEvent.Port;
                state.RetryCount = configEvent.RetryCount;
                state.ConfigVersion = configEvent.ConfigVersion;
                break;

            case TestExecutionSEvent executionEvent:
                state.TestExecutionCount++;
                state.LastTestTime = executionEvent.ExecutionTime;
                break;

            case ConfigurationStatusSEvent statusEvent:
                state.IsConfigured = statusEvent.IsConfigured;
                break;
        }
    }

    #region 私有验证方法

    private string ValidateStringType() => $"String validation: Name='{State.Name}', Length={State.Name.Length}, Valid={!string.IsNullOrEmpty(State.Name)}";
    private string ValidateIntType() => $"Int validation: MaxCount={State.MaxCount}, Type=System.Int32, Range=[{int.MinValue}, {int.MaxValue}]";
    private string ValidateBoolType() => $"Bool validation: IsEnabled={State.IsEnabled}, Type=System.Boolean";
    private string ValidateDoubleType() => $"Double validation: Precision={State.Precision}, Type=System.Double, Valid={State.Precision > 0}";
    private string ValidateDecimalType() => $"Decimal validation: Price={State.Price}, Type=System.Decimal, Valid={State.Price >= 0}";
    private string ValidateDateTimeType() => $"DateTime validation: StartDate={State.StartDate:yyyy-MM-dd}, Valid={State.StartDate != default}";
    private string ValidateTimeSpanType() => $"TimeSpan validation: Duration={State.Duration}, TotalMinutes={State.Duration.TotalMinutes}";
    private string ValidateEnumType() => $"Enum validation: Mode={State.Mode}, Valid={Enum.IsDefined(typeof(TestMode), State.Mode)}";
    private string ValidateGuidType() => $"Guid validation: SessionId={State.SessionId}, Valid={State.SessionId != Guid.Empty}";
    private string ValidateLongType() => $"Long validation: MaxFileSize={State.MaxFileSize}, Type=System.Int64";
    private string ValidateFloatType() => $"Float validation: Threshold={State.Threshold}, Type=System.Single, Range=[0,1]";
    private string ValidateUShortType() => $"UShort validation: Port={State.Port}, Type=System.UInt16, Valid={State.Port > 0}";
    private string ValidateByteType() => $"Byte validation: RetryCount={State.RetryCount}, Type=System.Byte, Range=[0,255]";
    private string ValidateShortType() => $"Short validation: ConfigVersion={State.ConfigVersion}, Type=System.Int16";
    
    private string ValidateAllTypes()
    {
        var validations = new[]
        {
            ValidateStringType(), ValidateIntType(), ValidateBoolType(), ValidateDoubleType(),
            ValidateDecimalType(), ValidateDateTimeType(), ValidateTimeSpanType(), ValidateEnumType(),
            ValidateGuidType(), ValidateLongType(), ValidateFloatType(), ValidateUShortType(),
            ValidateByteType(), ValidateShortType()
        };
        
        return string.Join("\n", validations);
    }

    #endregion
} 