# Agent 后续学习路线：Todo

更新时间：2026-07-25

## 1. 多轮任务状态机

优先级：中

目标：让 Agent 能维护一个任务生命周期，而不是每轮独立处理。

可设计字段：

- TaskId
- TaskStatus
- WaitingForUserInput
- WaitingForConfirmation
- Running
- Completed
- Cancelled
- Failed

## 2. RAG 检索增强生成

优先级：中

建议在当前 Evaluation 和日志体系稳定后再做。

需要学习：

- 文档切分 Chunking
- Embedding 向量化
- 向量数据库或向量索引
- 相似度检索
- Hybrid Search
- Rerank
- 引用来源
- 检索质量评估

博客系统应用：

- 跨文章问答
- 根据所有文章生成学习路线
- 查找相似文章
- 查询“哪些文章讲过某个主题”

## 3. 流式输出与执行进度

优先级：中低

目标：提升用户体验。

可以学习：

- SSE
- WebSocket
- Streaming Response
- 执行步骤进度
- 工具调用状态

## 4. 多 Agent / 子 Agent

优先级：后期

等单 Agent 足够稳定后再考虑。

可能拆分：

- Planner Agent
- Executor Agent
- Reviewer Agent
- Memory Agent
- Safety Agent
- Evaluation Agent

## 5. 生产级 Agent 运维

优先级：后期

后续可以学习：

- 日志保留策略
- 敏感信息脱敏
- 成本统计
- 用户配额
- 失败告警
- 模型降级
- 重试策略
- 缓存策略
- 幂等性设计
- 并发控制

## 6. 后续小清理

优先级：低

当前报告快照已经按“稳定审计数据”处理，不提供删除入口。

可选清理：

- 如果确定快照永不删除，可以移除 `AgentEvaluationReportSnapshot.IsDeleted`
- 移除对应索引和查询过滤
- 保持一轮 `AgentEvaluationRun` 只对应一份报告快照

## 7. 工具输入输出类型系统后续增强

优先级：中低

既有 12 个 Action 的 DTO 标准化已经完成，后续可以继续增强：

- Executor 按 Action 做更强类型分发
- 工具结果 Schema 校验
- 更统一的 Action Input 解析器
- 更统一的 Action Output 审计字段
- 减少残留的 `object Data` 与 JSON 正则兜底

意义：

```text
Action 越像稳定 API，Agent 越容易评估、审计和扩展。
```

## 推荐下一步

建议下一步继续：

```text
进入多轮任务状态机设计。
```

原因：

```text
长期记忆的读取、写入、去重更新、生命周期管理、主动遗忘和 Workflow 接入已经完成。
当前 Agent 已经具备跨会话记忆能力，下一步可以学习如何维护跨轮任务状态。
```

下一小步建议：

```text
1. 先区分“会话上下文”“长期记忆”和“当前任务状态”。
2. 设计任务状态表或状态对象，明确 TaskStatus 与等待条件。
3. 设计 Agent 何时创建任务、更新任务、完成任务或取消任务。
4. 再考虑任务状态如何与确认机制、WorkflowLog、PendingConfirmation 协作。
```
