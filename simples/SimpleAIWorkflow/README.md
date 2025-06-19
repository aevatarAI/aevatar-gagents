# SimpleAIWorkflow

本项目参考GroupChat分层结构，旨在演示基于Orleans Actor模型和GAgentFactory的AI工作流系统。

## 项目目标
- 构建一个可扩展的AI Workflow Agent示例。
- 所有成员通过GAgentFactory异步获取，支持灵活的Agent注入与扩展。
- Worker直接继承GroupMemberGAgentBase，支持自定义AI任务逻辑。
- 支持多步AI任务流、审批流等业务场景。

## 目录结构（初步规划）
- SimpleAIWorkflow.Client/    // 客户端入口，组装workflow，发起流程
- SimpleAIWorkflow.Silo/      // Orleans Silo宿主，注册服务
- SimpleAIWorkflow.Grains/    // 各类AI Agent实现（Coordinator/Worker等）

## Agent创建与绑定与编排流程（新版）
1. 首先创建一个业务Agent（如DataValidatorAgent），负责数据校验。
2. 然后创建多个CreatorGAgent，每个CreatorGAgent绑定一个WorkflowAIAgent（即每个CreatorGAgent管理一个独立的AI工作流节点）。
3. 最后通过WorkerFlowGAgent统一编排所有WorkflowAIAgent，实现完整的AI工作流链路。
4. 支持业务Agent、CreatorGAgent、WorkflowAIAgent、WorkerFlowGAgent的灵活扩展与组合。

## 讨论与细化
- 业务主题、角色分工、消息流动等细节待后续讨论确定。
- 欢迎补充具体需求或提出结构建议。 