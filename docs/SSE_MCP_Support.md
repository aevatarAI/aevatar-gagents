# SSE (Server-Sent Events) MCP Support

## Current Status

As of the latest update, SSE-based MCP servers have limited support in the Aevatar MCP implementation.

### What Works

1. **Connection**: SSE servers can be "connected" without errors
2. **Tool Discovery**: Predefined tools are returned for known SSE servers (e.g., zhipu-web-search-sse)
3. **Configuration**: SSE servers can be configured with proper transport type detection

### What Doesn't Work

1. **Tool Execution**: SSE tool calls are not implemented and will return an error message
2. **Real-time Events**: SSE event streams are not processed
3. **Authorization**: While authorization headers are extracted from URLs, the actual SSE connection doesn't use them properly

## Temporary Workaround

The `RealMCPClientProvider` includes a temporary workaround that:

1. Detects SSE endpoints by checking:
    - `TransportType` is "sse" (case-insensitive)
    - URL contains "/sse"

2. Skips the initialization phase for SSE endpoints

3. Returns predefined tools for known SSE servers:
    - zhipu-web-search: Returns a `web_search` tool with a `query` parameter

4. Returns error messages when attempting to call SSE tools

## Future Improvements

To fully support SSE MCP servers, the following improvements are needed:

### 1. Proper SSE Client Implementation
- Create a dedicated `SSEMCPClientProvider` class
- Use HttpClient with streaming support or a dedicated SSE library
- Handle SSE event parsing (event:, data:, id:, etc.)

### 2. Event Stream Processing
- Parse SSE events according to the specification
- Handle reconnection with Last-Event-ID
- Process different event types (message, error, etc.)

### 3. Authentication
- Move authorization from query parameters to headers
- Support Bearer token authentication
- Handle token refresh if needed

### 4. Async Event Handling
- Implement event handlers for real-time updates
- Support streaming responses for tool calls
- Handle connection lifecycle events

## Known SSE Servers

1. **zhipu-web-search-sse**
    - Endpoint: https://open.bigmodel.cn/api/mcp/web_search/sse
    - Authentication: Bearer token (from Authorization query parameter)
    - Tools: web_search

## Testing

When testing SSE connections:
1. The server will appear as "connected" even though full functionality is not available
2. Tools will be listed but cannot be executed
3. Error messages will clearly indicate SSE limitations