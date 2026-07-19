# Agent 后续学习路线：Todo

更新时间：2026-07-18

## 1. 新增 Action 扩展

优先级：中高

当前 12 个既有 Action 已完成 Input / Output DTO 标准化，并补齐了第一阶段 `MemoryFacts`。

下一阶段重点：

- 新增业务 Action
- 补 ConversationMemory 的最近文章列表能力
- 支持“第三篇”“最后一篇”“Redis 那篇”等列表指代
- 让文章查询、标签查询、推荐分类、推荐标签、发布文章形成完整链路

预期新增 Action：

1. `SelectArticleFromList`
   - 用途：从文章列表中选择某一篇文章，写入 ConversationMemory
   - 输入：`ListFromStep`、`Selection`
   - 输出：`ArticleId`、`Title`、`CategoryName`、`MatchMode`
   - 典型场景：用户说“第三篇”“选 Redis 那篇”

2. `SearchArticlesByKeyword`
   - 用途：按标题、摘要、正文或全部维度查询文章列表
   - 输入：`QueryText`、`SearchScope`、`ArticleScope`、`SortBy`、`Top`
   - 输出：`List<ArticleSearchResultItem>`、`TotalCount`、`SearchScope`、`ArticleScope`
   - 典型场景：用户说“标题包含 Redis 的文章”“内容里提到授权认证的文章”

3. `GetTagByName`
   - 用途：根据标签名查标签 ID
   - 输入：`TagName`
   - 输出：`TagId`、`TagName`、`CategoryId`、`CategoryName`

4. `SearchArticlesByTag`
   - 用途：根据标签 ID 查询文章列表
   - 输入：`TagId`、`SortBy`、`Top`
   - 输出：`List<ArticleSearchResultItem>`、`TagName`、`TotalCount`

5. `GetTagsByCategoryId`
   - 用途：根据分类 ID 查询该分类下的标签列表
   - 输入：`CategoryId`
   - 输出：`CategoryId`、`CategoryName`、`CategoryDescription`、`List<TagItem>`

6. `RecommendCategory`
   - 用途：根据内容推荐分类
   - 输入：`Content`、`Title`
   - 输出：`RecommendedCategoryId`、`RecommendedCategoryName`、`Confidence`、`Reason`
   - 注意：不写入库，如果是最后一步 Action，只返回建议文本

7. `RecommendTags`
   - 用途：根据内容推荐标签
   - 输入：`Content`、`Title`、`ExistingTags`
   - 输出：`List<RecommendedTag>`
   - 注意：不写入库，如果是最后一步 Action，只返回建议文本

8. `CreateArticle`
   - 用途：发布文章
   - 输入：`Title`、`Content`、`Summary`、`CategoryId`、`TagIds`、`Description`
   - 输出：`ArticleId`、`Title`、`CategoryName`、`CreatedAt`、`ContentLength`
   - 注意：需要确认，走 `RequireConfirmation`，并且需要 plan 参数校验、AI 生成结果校验、写入前风险校验

建议实现顺序：

```text
1. 补 ConversationMemory 的最近文章列表能力
2. SelectArticleFromList
3. SearchArticlesByKeyword
4. GetTagByName
5. SearchArticlesByTag
6. GetTagsByCategoryId
7. RecommendCategory
8. RecommendTags
9. CreateArticle
```

新增 Action 接入检查项：

- `AgentActionRegistry`
- Input DTO / Output DTO
- `IAgentActionOutput`
- `IAgentNaturalLanguageOutput`，如果最后一步可直接回答
- `IAgentMemoryFactProvider`
- Executor 分支
- Planner Prompt
- Action 校验白名单
- 风险等级
- 参数权限校验
- 最终回答生成
- WorkflowLog / Evaluation 记录
- 至少 1 个评估用例

意义：

```text
让 Agent 支持“查到一组文章 -> 用户从列表中选择某一篇 -> 对选中文章继续查询、润色、修改或发布相关操作”的完整链路。
```

## 2. 更强的长期 Memory 策略

优先级：中（暂缓）

当前 Memory 已能处理会话上下文与文章指代，后续可以学习长期记忆。

暂缓原因：

```text
当前第一阶段记忆策略已足够支撑文章指代与最近上下文。
先扩展更多 Action，等业务场景更丰富后，再回头设计更强长期 Memory 会更自然。
```

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
- 如果用户想按列表序号、标题关键词或正文关键词选中文章，目前能力还不够自然

已并入“新增 Action 扩展”的第一阶段核心能力：

- `SelectArticleFromList`
- `SearchArticlesByKeyword`

建议第一阶段先做：

```text
SelectArticleFromList
```

设计方向：

- 列表型 Action 返回多篇文章时，只写入 `MentionedArticles`
- `SelectArticleFromList` 根据 `ListFromStep` 和 `Selection` 选中具体文章
- 选中后写入 `LastSelectedArticleId`、`LastSelectedArticleTitle`
- 支持 `ByIndex`、`ByTitle`、`NotFound`

意义：

```text
让“第三篇”“最后一篇”“Redis 那篇”这类自然表达，可以从最近文章列表中稳定定位到具体文章。
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

## 10. 工具输入输出类型系统后续增强

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
补 ConversationMemory 的最近文章列表能力
```

原因：

```text
已有 Action 的 DTO 与 MemoryFacts 第一阶段已经收尾。
但列表型 Action 目前只能把文章作为 Mentioned，不足以支持“第三篇”“最后一篇”这类自然选择。
进入 SelectArticleFromList 之前，需要先让 ConversationMemory 能保存最近一次文章列表。
```

下一小步建议：

```text
先设计 RecentMentionedArticlesJson 或类似字段。
保存最近一次列表型 Action 返回的文章候选。
再实现 SelectArticleFromList，让用户可以从候选列表中选择具体文章。
```
