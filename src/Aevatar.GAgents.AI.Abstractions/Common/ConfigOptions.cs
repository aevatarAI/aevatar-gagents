using System;
using System.Collections.Generic;
using System.Linq;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// LLM模型选项管理
/// </summary>
public static class LLMModelOptions
{
    /// <summary>
    /// 系统LLM配置选项
    /// </summary>
    public static class SystemLLM
    {
        public const string OpenAI = "OpenAI";
        public const string DeepSeek = "DeepSeek";
        public const string Azure = "Azure";
        public const string Gemini = "Gemini";
        
        public static readonly string[] AvailableOptions = { OpenAI, DeepSeek, Azure, Gemini };
        public static string Default => OpenAI;
        
        public static bool IsValid(string value) => AvailableOptions.Contains(value);
        
        public static string GetDescription(string value) => value switch
        {
            OpenAI => "OpenAI GPT models (gpt-4, gpt-3.5-turbo)",
            DeepSeek => "DeepSeek models optimized for reasoning",
            Azure => "Azure OpenAI service models",
            Gemini => "Google Gemini models",
            _ => "Unknown model"
        };
    }
    
    /// <summary>
    /// 模型名称选项
    /// </summary>
    public static class ModelNames
    {
        public const string GPT4O = "gpt-4o";
        public const string GPT35Turbo = "gpt-3.5-turbo";
        public const string GPT4 = "gpt-4";
        public const string DeepSeekR1 = "DeepSeek-R1";
        public const string Gemini15Pro = "gemini-1.5-pro";
        public const string DallE3 = "dall-e-3";
        
        public static readonly string[] AvailableOptions = 
        { 
            GPT4O, GPT35Turbo, GPT4, DeepSeekR1, Gemini15Pro, DallE3 
        };
        
        public static string Default => GPT4O;
        
        public static bool IsValid(string value) => AvailableOptions.Contains(value);
    }
    
    /// <summary>
    /// LLM提供商枚举选项
    /// </summary>
    public static class ProviderOptions
    {
        public static readonly LLMProviderEnum[] AvailableOptions = 
        {
            LLMProviderEnum.Azure,
            LLMProviderEnum.OpenAI,
            LLMProviderEnum.DeepSeek,
            LLMProviderEnum.Google
        };
        
        public static LLMProviderEnum Default => LLMProviderEnum.Azure;
        
        public static bool IsValid(LLMProviderEnum value) => Enum.IsDefined(typeof(LLMProviderEnum), value);
    }
    
    /// <summary>
    /// 模型ID枚举选项
    /// </summary>
    public static class ModelIdOptions
    {
        public static readonly ModelIdEnum[] AvailableOptions = 
        {
            ModelIdEnum.OpenAI,
            ModelIdEnum.DeepSeek,
            ModelIdEnum.Gemini,
            ModelIdEnum.OpenAITextToImage
        };
        
        public static ModelIdEnum Default => ModelIdEnum.OpenAI;
        
        public static bool IsValid(ModelIdEnum value) => Enum.IsDefined(typeof(ModelIdEnum), value);
    }
}

/// <summary>
/// 数值选项管理
/// </summary>
public static class NumericOptions
{
    /// <summary>
    /// 重试次数选项
    /// </summary>
    public static class RetryCount
    {
        public static readonly int[] AvailableOptions = { 1, 3, 5, 10 };
        public static int Default => 3;
        public static bool IsValid(int value) => AvailableOptions.Contains(value);
    }
    
    /// <summary>
    /// 端口号选项
    /// </summary>
    public static class PortNumbers
    {
        public static readonly ushort[] AvailableOptions = { 8080, 3000, 5000, 8000, 9000 };
        public static ushort Default => 8080;
        public static bool IsValid(ushort value) => AvailableOptions.Contains(value);
    }
    
    /// <summary>
    /// 超时时间选项（秒）
    /// </summary>
    public static class TimeoutSeconds
    {
        public static readonly int[] AvailableOptions = { 30, 60, 120, 300, 600 };
        public static int Default => 100;
        public static bool IsValid(int value) => AvailableOptions.Contains(value);
    }
    
    /// <summary>
    /// 最大连接数选项
    /// </summary>
    public static class MaxConnections
    {
        public static readonly int[] AvailableOptions = { 10, 50, 100, 200, 500 };
        public static int Default => 100;
        public static bool IsValid(int value) => AvailableOptions.Contains(value);
    }
    
    /// <summary>
    /// 历史记录数量选项
    /// </summary>
    public static class HistoryCount
    {
        public static readonly int[] AvailableOptions = { 5, 10, 20, 50, 100 };
        public static int Default => 20;
        public static bool IsValid(int value) => AvailableOptions.Contains(value);
    }
}

/// <summary>
/// API端点选项管理
/// </summary>
public static class EndpointOptions
{
    /// <summary>
    /// API端点预设
    /// </summary>
    public static class Common
    {
        public const string LocalHost = "http://localhost:8080";
        public const string TestEnvironment = "https://test-api.example.com";
        public const string ProductionEnvironment = "https://api.example.com";
        public const string AzureOpenAI = "https://youraccount.openai.azure.com";
        
        public static readonly string[] AvailableOptions = 
        {
            LocalHost, TestEnvironment, ProductionEnvironment, AzureOpenAI
        };
        
        public static string Default => TestEnvironment;
        
        public static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value) && Uri.IsWellFormedUriString(value, UriKind.Absolute);
    }
}

/// <summary>
/// 时间间隔选项管理
/// </summary>
public static class TimeSpanOptions
{
    /// <summary>
    /// 常用时间间隔选项
    /// </summary>
    public static class Common
    {
        public static readonly TimeSpan[] AvailableOptions = 
        {
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(6),
            TimeSpan.FromDays(1)
        };
        
        public static TimeSpan Default => TimeSpan.FromMinutes(30);
        
        public static bool IsValid(TimeSpan value) => AvailableOptions.Contains(value);
        
        public static string GetDescription(TimeSpan value) => value.TotalDays >= 1 
            ? $"{value.TotalDays:0} days" 
            : value.TotalHours >= 1 
                ? $"{value.TotalHours:0} hours" 
                : $"{value.TotalMinutes:0} minutes";
    }
}

/// <summary>
/// 精度/阈值选项管理
/// </summary>
public static class PrecisionOptions
{
    /// <summary>
    /// 浮点数精度选项
    /// </summary>
    public static class FloatPrecision
    {
        public static readonly float[] AvailableOptions = { 0.1f, 0.5f, 0.7f, 0.8f, 0.9f, 0.95f, 0.99f };
        public static float Default => 0.95f;
        public static bool IsValid(float value) => AvailableOptions.Contains(value);
    }
    
    /// <summary>
    /// 双精度选项
    /// </summary>
    public static class DoublePrecision
    {
        public static readonly double[] AvailableOptions = { 0.1, 0.5, 0.7, 0.8, 0.9, 0.95, 0.99 };
        public static double Default => 0.8;
        public static bool IsValid(double value) => AvailableOptions.Contains(value);
    }
}

/// <summary>
/// 图像生成选项管理
/// </summary>
public static class ImageOptions
{
    /// <summary>
    /// 图像风格选项
    /// </summary>
    public static class Style
    {
        public static readonly TextToImageStyleEnum[] AvailableOptions = 
        {
            TextToImageStyleEnum.Vivid,
            TextToImageStyleEnum.Natural
        };
        
        public static TextToImageStyleEnum Default => TextToImageStyleEnum.Vivid;
        
        public static bool IsValid(TextToImageStyleEnum value) => Enum.IsDefined(typeof(TextToImageStyleEnum), value);
    }
    
    /// <summary>
    /// 图像质量选项
    /// </summary>
    public static class Quality
    {
        public static readonly TextToImageQualityEnum[] AvailableOptions = 
        {
            TextToImageQualityEnum.Standard,
            TextToImageQualityEnum.High,
            TextToImageQualityEnum.HD
        };
        
        public static TextToImageQualityEnum Default => TextToImageQualityEnum.Standard;
        
        public static bool IsValid(TextToImageQualityEnum value) => Enum.IsDefined(typeof(TextToImageQualityEnum), value);
    }
} 