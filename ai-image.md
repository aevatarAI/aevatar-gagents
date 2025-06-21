
## 针对下面的需求，输出方案文档，project_tracker.md文档，便于后续开发。

### 需求
@AIGAgentBase.cs 和 @ChatGAgentBase.cs 在现有支持文本的基础上支持图片。

### 方案方向
- 在 @AIGAgentBase.cs 通过使用 vision model 支持带图片的聊天(当前代码文本的聊天的gpt-4o已经支持了图片)
- @AIGAgentBase.cs 和 @ChatGAgentBase.cs 除现在文本外也支持传入图片id列表。@AIGAgentBase.cs通过云Blob获取图片，并使用vision model聊天
- 获取云Blob抽象接口，并实现aws blob。
- 在AIGagentBase中支持同步和异步处理，参考现有文本处理的实现

### 方案输出要求
- 输出格式为Markdown
- 包含架构设计及流程图(使用mermaid)
- 尽可能少包含代码