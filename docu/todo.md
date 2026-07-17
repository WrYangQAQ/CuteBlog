# Agent 后续学习路线：Todo

更新时间：2026-07-16

## 1. 工具输入输出类型系统

优先级：中

目标：减少 object / JSON 猜测，让 Action 更像稳定 API。

当前进度：

- 已开始为 Action 拆分独立 Input / Output DTO
- 已完成 7 个 Action 的 DTO 标准化
- 已完成 `SearchArticlesByCategory`
- 已完成 `GetArticleContentById`
- 已完成 `SummarizeContent`
- 已完成 `AnswerQuestionFromContent`
- 已完成 `CompareContents`
- 已完成 `GetMyArticles`
- 已完成 `UpdateArticleTitle`
- `ArticleSortBy` 已统一为枚举表达
- 已引入 `IUserReadableOutput`
- 已引入 `IAgentContentOutput`
- 已引入 `IAgentArticleReferenceOutput`
- 已引入 `AgentMemoryFact`，避免 MemoryService 直接解析各种 Output DTO

剩余待 DTO 标准化的 Action：

- `GetAllCategories`
- `ExplainFailureWithSuggestions`
- `GenerateContentRevision`
- `UpdateArticleContent`
- `DeleteArticle`

可以实现：

- 继续为剩余 Action 独立 Input DTO
- 继续为剩余 Action 独立 Output DTO
- Executor 按 Action 做强类型分发
- FinalAnswer 根据 Output 类型生成回答
- 工具结果 Schema 校验
- 逐步减少 `object Data` 与 JSON 正则猜测

意义：

```text
Agent 的工具层越稳定，最终回答越可靠。
```

## 2. 更强的长期 Memory 策略

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

## 3. 文章定位能力增强

优先级：中

当前情况：

- `articleIdFromStep` 默认取上一步结果中的第一篇文章
- 适合“点赞最高 / 浏览量最高 / 最新”这类排序后取第一项的任务
- 如果用户想按标题、关键词或更具体条件选中文章，目前能力还不够自然

后续可以新增 Action：

- `FindMyArticleByTitle`
- 或更通用的 `FindMyArticle`

建议第一阶段先做：

```text
FindMyArticleByTitle
```

设计方向：

- 输入：`TitleKeyword`
- 输出：匹配文章列表
- 只在唯一匹配时实现 `IAgentArticleReferenceOutput.GetPrimaryArticleId`
- 多篇匹配时不自动修改，让 Agent 追问用户选择哪一篇

意义：

```text
让“把我那篇 Redis 文章改名”这类自然表达，可以先定位文章，再进入确认和写操作。
```

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

建议下一步继续：

```text
工具输入输出类型系统
```

原因：

```text
目前已完成 7 个 Action，还剩 5 个 Action。
把现有 Action 的 DTO 地基铺完，再回头新增文章定位类 Action，会更稳。
```

下一小步建议：

```text
继续按 Action 逐个拆 Input / Output DTO。
优先处理 GenerateContentRevision、UpdateArticleContent。
```
