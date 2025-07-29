using System.Text.Json;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.GEvents;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Aevatar.GAgents.MCP.GAgents;

// ReSharper disable MemberCanBePrivate.Global
public abstract partial class MCPGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
{
    public async Task<MCPToolResponseEvent> HandleEventAsync(MCPToolCallEvent toolCallEvent)
    {
        try
        {
            // Extract server name and actual tool name
            var parts = toolCallEvent.ToolName.Split('.');
            if (parts.Length < 2)
            {
                throw new ArgumentException($"Invalid tool name: {toolCallEvent.ToolName}");
            }

            var actualToolName = string.Join(".", parts.Skip(1));

            // Set timeout
            using var cts = new CancellationTokenSource(State.RequestTimeout);

            // Call tool through official SDK
            var arguments = toolCallEvent.Arguments.ToDictionary(kvp => kvp.Key, object? (kvp) => kvp.Value);
            var result = await McpClient.CallToolAsync(actualToolName, arguments, cancellationToken: cts.Token);

            var response = new MCPToolResponseEvent
            {
                RequestId = toolCallEvent.RequestId,
                Success = true,
                Result = new MCPToolCallResult
                {
                    Success = true,
                    Data = ExtractContentFromMcpResult(result),
                    ErrorMessage = null
                }
            };

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to call mcp tool: {ToolName}.", toolCallEvent.ToolName);

            var errorResponse = new MCPToolResponseEvent
            {
                RequestId = toolCallEvent.RequestId,
                Success = false,
                Result = new MCPToolCallResult
                {
                    Success = false,
                    Data = null,
                    ErrorMessage = ex.Message
                }
            };

            return errorResponse;
        }
    }

    public async Task<MCPToolsDiscoveredEvent> HandleEventAsync(MCPDiscoverToolsEvent discoverEvent)
    {
        Logger.LogInformation("Start to execute event handler for MCPDiscoverToolsEvent.");
        try
        {
            var allTools = new List<MCPToolInfo>();
            var tools = await McpClient.ListToolsAsync();
            allTools.AddRange(tools.Select(t => ConvertToMCPToolInfo(t, State.MCPServerConfig.ServerName)));

            return new MCPToolsDiscoveredEvent
            {
                ServerName = State.MCPServerConfig.ServerName,
                Tools = allTools
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to discover tools.");
            throw;
        }
    }

    /// <summary>
    /// Extract content text from MCP result
    /// </summary>
    private string ExtractContentFromMcpResult(object result)
    {
        try
        {
            // Use reflection to get Content property
            var resultType = result?.GetType();
            if (resultType == null)
            {
                return string.Empty;
            }

            var contentProperty = resultType.GetProperty("Content");
            if (contentProperty == null)
            {
                return string.Empty;
            }

            var contentValue = contentProperty.GetValue(result);
            if (contentValue == null)
            {
                return string.Empty;
            }

            // Try to get the first content item
            object? firstContent = null;
            if (contentValue is System.Collections.IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    firstContent = enumerator.Current;
                }
            }

            if (firstContent == null)
            {
                return string.Empty;
            }

            // 尝试获取Text属性（TextContentBlock应该有这个属性）
            var contentType = firstContent.GetType();
            var textProperty = contentType.GetProperty("Text");
            if (textProperty != null)
            {
                var textValue = textProperty.GetValue(firstContent);
                return textValue?.ToString() ?? string.Empty;
            }

            // 如果没有Text属性，尝试Value属性
            var valueProperty = contentType.GetProperty("Value");
            if (valueProperty != null)
            {
                var value = valueProperty.GetValue(firstContent);
                return value?.ToString() ?? string.Empty;
            }

            // 如果都没有，尝试将整个对象序列化为JSON
            var json = JsonSerializer.Serialize(firstContent);
            Logger.LogDebug("MCP content serialized as: {Json}", json);
            
            // 尝试从JSON中提取text字段
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("text", out JsonElement textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }
            if (doc.RootElement.TryGetProperty("Text", out JsonElement TextElement))
            {
                return TextElement.GetString() ?? string.Empty;
            }
            
            // 如果还是找不到，返回整个JSON
            return json;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to extract content from MCP result");
            // 降级到ToString()
            return result?.ToString() ?? string.Empty;
        }
    }
}