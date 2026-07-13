# Agent 开发阶段总结：Done

更新时间：2026-07-04

## 1. Agent 主流程

已经完成一条较完整的博客 Agent 执行链路：

```text
用户输入 -> 保存消息 -> 意图识别 -> 构建上下文 -> 生成 Plan -> 校验 Plan
-> 执行 Action -> 失败分析 / 补救 -> 最终回答 -> 保存日志 -> 更新记忆 -> 前端展示
```

已实现能力：

- Planner 生成结构化计划
- Plan Validator 校验计划合法性
- Plan Repair 修复不合法计划
- Executor 执行业务 Action
- 失败分析与补救计划
- 最终回答生成
- 异常捕获与超时控制

## 2. Action 与安全边界

已经完成 Action 的基础工程化管理：

- Action 名称集中管理
- AllowedActions 白名单限制
- RecoveryActions 限制
- Action 风险等级划分
- ReadOnly / RequireConfirmation / Forbidden
- 未知 Action 默认禁止
- 高风险操作需要用户确认

核心认识：

```text
LLM 可以提出计划，但系统必须控制边界。
```

## 3. 会话、消息与记忆系统

已经完成从简单 Memory 表到会话架构的升级：

- AgentConversations
- AgentMessages
- AgentConversationMemories
- 会话标题
- 会话状态 Active / Archived / Deleted / Evaluation
- User / Assistant / System 消息角色
- TokenCount
- 会话归档、恢复、删除
- 前端会话列表

记忆能力已支持：

- 最近上下文构建
- 上一轮用户问题与回答
- 上一轮选中文章 ID / 标题
- ConversationSummary
- LastSummarizedMessageId
- ContextResetAt / ContextResetMessageId
- 上下文重置
- 历史消息压缩
- 指代理解，例如“它、这篇、刚才那篇文章”

## 4. 意图路由

已经完成基础意图分流：

- ExecuteWorkflow：进入完整 Agent 工作流
- DirectChat：普通闲聊或感谢语
- ResetContext：清除上下文
- Unsupported：拒绝不支持或敏感请求

这样避免了所有输入都盲目进入 Planner。

## 5. Workflow 日志系统

已经完成工作流日志落库与前端查看：

- AgentWorkflowLogs 表
- 保存 UserMessage
- 保存 Success / Recovered / Message / Answer
- 保存 DurationMs / StartedAt / FinishedAt
- 保存 PlanJson
- 保存 ExecutionResultJson
- 保存 FailureAnalysis
- 保存 RecoveryPlanJson
- 保存 RecoveryExecutionResultJson
- 前端执行日志弹窗
- 日志详情查看

## 6. 用户确认机制

已经完成 Human-in-the-loop 确认机制：

- AgentPendingConfirmations 表
- ConfirmationId
- Pending / Confirmed / Cancelled / Expired
- 确认过期时间
- 高风险 Plan 暂停执行
- 前端确认 / 取消按钮
- 用户确认后继续执行原 Plan
- 用户取消或过期后不执行

## 7. 前端 Agent 体验

已经完成博客助手页面的主要交互：

- Agent 对话框
- 消息展示
- 会话侧边栏
- 活跃 / 已归档切换
- 三点菜单
- 归档 / 恢复 / 删除
- 高风险确认卡片
- 执行日志入口
- 管理员评估中心入口

## 8. Agent Evaluation 评估体系

已经完成一套数据库驱动的评估系统：

- AgentTestCase：测试用例库
- AgentEvaluationRun：评估批次
- AgentEvaluationResult：评估结果
- 测试用例启用 / 禁用 / 新增 / 编辑 / 删除
- 批量运行全部用例
- 运行选中用例
- 结果落库
- Evaluation 会话状态标记
- 语义评估 Judge
- 关键词兜底
- 失败类型 FailureType
- 单条失败分析
- 评估结果关联 WorkflowLogId
- 根据 runId + caseId 查询对应 WorkflowLog

## 9. Evaluation 版本与报告

已经完成评估版本标记和报告基础闭环：

- AgentEvaluationRun 保存 PlannerPromptVersion
- AgentEvaluationRun 保存 ActionRegistryVersion
- AgentEvaluationRun 保存 EvaluationVersion
- AgentEvaluationRun 保存 FinalAnswerPromptVersion
- Markdown 评估报告生成
- 报告中展示 Prompt / Action / Evaluation / FinalAnswer 版本
- 报告中展示失败用例 WorkflowLogId
- 报告中展示失败分析建议
- 报告预览弹窗
- 复制评估报告
- 下载 `.md` 评估报告

## 10. Evaluation 前端中心

当前评估中心前端已支持：

- 测试用例与评估批次分 Tab 展示
- 用例卡片管理
- 新增用例弹窗
- 编辑用例弹窗
- 删除 / 启用 / 禁用用例
- 批次列表
- 结果详情
- 总数 / 通过 / 失败统计
- 失败类型分布
- 查看对应执行日志
- 复制 / 预览 / 下载后端生成的 Markdown 报告
- 批次对比视图
- 回归摘要展示
- 快照重跑按钮
- 保存报告快照
- 查看报告快照

## 11. 评估批次对比与回归分析

已经完成：

- 两个 EvaluationRun 的批次对比
- 恢复通过 / 退化 / 持续通过 / 持续失败 / 新增缺失统计
- 单个 Case 的基准结果与目标结果对照
- 语义分数变化展示
- 后端生成回归摘要
- 前端展示回归结论、亮点、风险和下一步建议

## 12. Evaluation 可复现性

已经补上旧 Run 的可复现能力：

- `AgentEvaluationResult` 保存 `TestCaseSnapshotJson`
- 快照中保留当时测试用例的核心字段
- 即使后续修改 `AgentTestCase`，旧 Run 仍能知道当时测了什么
- 基于旧 Run 的结果快照重新构造评估请求

核心意义：

```text
旧 Run 不再依赖当前 AgentTestCase 表，也能复现当时的测试输入与预期。
```

## 13. 基于历史 Run 的快照重跑

已经完成历史批次复现链路：

- 根据旧 RunId 查询评估结果
- 从 `TestCaseSnapshotJson` 反序列化当时用例
- 创建新的 EvaluationRun
- 重新执行评估
- 新旧批次可以继续对比
- 新 Run 的 `SourceId` 指向来源 Run

当前规则：

```text
普通评估 SourceId 为 null。
基于快照重跑的评估 SourceId 为来源批次 Id。
```

## 14. Evaluation 报告快照与审计链

已经完成报告快照能力：

- 新增 `AgentEvaluationReportSnapshot`
- 一轮 Run 保存一份报告快照
- 快照保存 RunId、FileName、MarkdownContent
- 快照保存四个版本号
- 快照保存 CreatedAt
- 前端支持保存快照
- 前端支持查看快照
- 快照作为稳定审计数据，不提供删除入口

当前 Evaluation 审计链已经形成：

```text
Run -> Result -> TestCaseSnapshotJson -> WorkflowLogId -> ReportSnapshot -> SourceId
```

可以回答：

- 这次评估用了什么用例
- 这次评估用了什么 Prompt / Action / Judge / FinalAnswer 版本
- 这次评估是否复现旧批次
- 这次评估的报告归档内容是什么

## 15. 当前阶段定位

当前项目的 Agent 学习阶段大致处于：

```text
Agent 工程化中高级阶段
```

已经不只是实现“让模型回答问题”，而是在构建：

```text
可控、可观察、可评估、可审计、可复现、可迭代的 Agent 系统。
```