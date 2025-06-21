## 方案概述

为了满足用户上传图片作为聊天上下文的需求，本方案将扩展现有的AIGAgent架构，通过集成Vision Model支持，实现图像与文本的多模态处理能力。用户可以上传图片到云存储，然后通过Blob ID与文本提示一起发送给AI Agent进行处理。

## 架构设计

### 核心组件扩展

#### 1. BrainContent扩展
在现有的BrainContentType枚举中新增Image和ImageUrl类型，支持图像内容的标识和处理。

#### 2. ChatMessage扩展  
为ChatMessage类添加图像内容列表字段，支持单条消息包含多个图像附件。

#### 3. Vision Brain接口
创建IVisionBrain接口继承IChatBrain，新增带图像参数的提示调用方法，支持同步和流式两种处理模式。

## 系统流程设计

### 整体流程图

```mermaid
graph TB
    A[用户上传图片] --> B[云Blob存储]
    B --> C[返回BlobId]
    C --> D[发送Prompt + BlobId]
    D --> E[AIGAgentBase接收请求]
    E --> F[从Blob获取图片]
    F --> G[图片预处理/验证]
    G --> H{图片处理结果}
    H -->|成功| I[Vision Model处理]
    H -->|失败| J[返回错误信息]
    I --> K[流式响应处理]
    K --> L[返回AI回复]
    J --> L
```

### 详细处理流程

```mermaid
sequenceDiagram
    participant Client as 客户端应用
    participant Blob as 云存储服务
    participant Agent as AIGAgentBase
    participant LLM as 多模态LLM
    
    Client->>Blob: 上传图片
    Blob-->>Client: 返回BlobId
    
    Client->>Agent: 发送消息(文本+BlobId)
    Agent->>Blob: 根据BlobId获取图片
    Blob-->>Agent: 返回图片数据
    
    Blob->>LLM: 发送多模态请求
    
    LLM-->>Agent: 返回响应
    Agent-->>Client: 返回响应
    
```

## 关键技术实现点

### 1. 图像获取与处理
- **Blob存储集成**：支持Azure Blob Storage、AWS S3等云存储服务
- **图像格式验证**：支持JPEG、PNG、WebP等常见格式，确保兼容性
- **安全检查**：自动清理EXIF元数据，检测并阻止恶意文件上传
- **尺寸优化**：自动压缩过大图片以适应LLM输入限制，保持图像质量

### 2. 多模态Brain实现
- **Vision Model集成**：支持GPT-4V、Gemini Vision等主流视觉模型
- **上下文管理**：维护图片与对话历史的关联关系，保证上下文连贯性

### 3. 流式处理扩展
- **异步图片处理**：图片处理不阻塞主流程，提升响应速度
- **错误恢复**：图片处理失败时优雅降级到纯文本模式

### 4. 存储与清理策略  
- **TTL管理**：支持可配置的图片保留时间，平衡存储成本与用户体验
- **自动清理**：定时清理过期图片，防止存储空间膨胀

## 配置与管理

### Blob存储配置
支持连接字符串、容器名称、TTL时间、最大文件大小、允许的MIME类型等全面配置选项。

### Vision处理配置  
提供图像处理开关、单消息最大图片数量、最大图像分辨率、图像嵌入功能、支持格式列表等详细配置。

## 总结

本方案通过扩展现有的AIGAgent架构，实现了完整的多模态AI聊天功能。方案重点关注：

1. **架构扩展性**：基于现有框架进行最小化侵入式扩展，保持系统稳定性
2. **性能优化**：流式处理与异步操作确保优秀的用户体验
3. **安全可靠**：完善的错误处理与多层次安全防护机制
4**用户体验**：直观简洁的交互设计与友好的错误处理

通过这个方案，开发者可以轻松地为他们的AI Agent添加图像处理能力，用户也能获得更丰富的多模态交互体验。该方案具备良好的扩展性和可维护性，能够适应未来多模态AI技术的发展需求。
