# Agent 后续学习路线：Todo

更新时间：2026-07-04

## 1. 参数级权限与风险控制

优先级：中高

当前已经做到 Action 级风险控制，后续可以升级到参数级。

示例：

- 查询文章：ReadOnly
- 删除自己的草稿：RequireConfirmation
- 删除自己已发布文章：RequireConfirmation + 二次确认
- 删除他人文章：Forbidden
- 批量删除：Forbidden 或管理员确认

需要学习：

- 用户权限判断
- 资源归属判断
- 参数级风险识别
- 高风险操作审计
- 风险判断结果写入 WorkflowLog 或确认记录

目标：

```text
不只判断“这个 Action 能不能执行”，还要判断“这次参数下该不该执行”。
```

## 2. 工具输入输出类型系统

优先级：中

目标：减少 object / JSON 猜测，让 Action 更像稳定 API。

可以实现：

- 每个 Action 独立 Input DTO
- 每个 Action 独立 Output DTO
- Executor 按 Action 做强类型分发
- FinalAnswer 根据 Output 类型生成回答
- 工具结果 Schema 校验

意义：

```text
Agent 的工具层越稳定，最终回答越可靠。
```

## 3. 更强的长期 Memory 策略

优先级：中

当前 Memory 已能处理会话上下文与文章指代，后续可以学习长期记忆。

方向：

- 用户偏好记忆
- 用户学习主题
- 最近处理任务
- 重要事实抽取
- 记忆重要性评分
- 记忆过期策略
- 记忆冲突处理
- 向量记忆

## 4. 多轮任务状态机

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

## 5. RAG 检索增强生成

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

## 6. 流式输出与执行进度

优先级：中低

目标：提升用户体验。

可以学习：

- SSE
- WebSocket
- Streaming Response
- 执行步骤进度
- 工具调用状态

## 7. 多 Agent / 子 Agent

优先级：后期

等单 Agent 足够稳定后再考虑。

可能拆分：

- Planner Agent
- Executor Agent
- Reviewer Agent
- Memory Agent
- Safety Agent
- Evaluation Agent

## 8. 生产级 Agent 运维

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

## 9. 后续小清理

优先级：低

当前报告快照已经按“稳定审计数据”处理，不提供删除入口。

可选清理：

- 如果确定快照永不删除，可以移除 `AgentEvaluationReportSnapshot.IsDeleted`
- 移除对应索引和查询过滤
- 保持一轮 `AgentEvaluationRun` 只对应一份报告快照

## 推荐下一步

建议下一步进入：

```text
参数级权限与风险控制
```

原因：

```text
当前 Agent 已经能计划、执行、确认、评估和复现。
下一步应该让系统能根据参数、用户身份和资源归属判断风险，进一步提升安全边界。
```