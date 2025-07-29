using Orleans;

namespace Aevatar.GAgents.MCP.Core.Model;

/// <summary>
/// MCP工具调用结果
/// </summary>
[GenerateSerializer]
public class MCPToolCallResult
{
    /// <summary>
    /// 调用是否成功
    /// </summary>
    [Id(0)]
    public bool Success { get; set; }

    /// <summary>
    /// 返回数据
    /// </summary>
    [Id(1)]
    public string? Data { get; set; }

    /// <summary>
    /// 错误消息（如果有）
    /// </summary>
    [Id(2)]
    public string? ErrorMessage { get; set; }
}