# Agent 开发阶段总结：Done

更新时间：2026-07-18

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

## 15. Action 扩展与写操作确认

已经完成文章写操作相关 Action 的第一轮扩展：

- `GetMyArticles`
- `UpdateArticleTitle`
- `GenerateContentRevision`
- `UpdateArticleContent`
- `DeleteArticle`

当前策略：

- 修改标题：需要确认后执行
- 修改文章内容：需要确认后执行
- 删除文章：注册为 Action，但当前保持 `Forbidden`
- 查询文章列表：只读操作
- 内容修订：只生成候选内容，不直接写库

已经验证：

- 修改自己的文章标题可以进入确认流程并成功执行
- 修改他人文章会被权限校验拒绝
- 删除文章请求会被安全边界拒绝

## 16. 参数级权限控制

已经完成参数级权限校验的基础能力：

- 根据 Action 和参数判断资源归属
- `UpdateArticleTitle` 校验文章是否属于当前用户
- `UpdateArticleContent` 校验文章是否属于当前用户
- 拦截越权修改他人文章
- 禁止用户通过参数注入 `userId`、`role`、`isAdmin`、`ownerId` 等身份字段
- 参数 key 使用大小写不敏感处理
- 确认执行与正常执行都会经过参数权限校验

核心认识：

```text
Action 允许执行，不代表这一次参数允许执行。
```

## 17. 参数级风险控制

已经完成参数语义风险嗅探：

- 新标题为空或过短时拦截
- 新标题包含换行时拦截
- 新正文直接为空时拦截
- 新正文过短、占位词、疑似清空时拦截
- 查询数量异常大时拦截
- 敏感或破坏性关键词拦截
- `AgentParameterRiskService` 职责明确为“参数语义安检”

已经明确分层：

```text
PlanValidator：检查结构是否合法。
PermissionService：检查用户能不能操作这个资源。
RiskService：检查参数值是否像误操作或高风险输入。
Executor：真正执行 Action。
```

## 18. 执行期结果风险控制

已经完成执行期真实结果风险控制：

- `UpdateArticleContent` 在写库前解析真实 `newContent`
- `newContentFromStep` 会先从前置步骤结果中取出真实内容
- 写库前调用参数风险服务进行最后校验
- 拦截空正文、极短正文、占位词正文
- 拦截疑似清空、覆盖、破坏性生成结果
- `GenerateContentRevision` 的高风险修改指令也会被校验
- 正常执行与确认执行都保留风险校验
- 执行失败会进入 WorkflowLog 与 Evaluation 链路
- 已新增并通过 Case 7：高风险清空正文安全拦截

当前形成了三层防线：

```text
计划阶段参数风险校验 -> 确认执行前再次校验 -> 执行期真实结果写库前校验
```

核心认识：

```text
计划看起来安全，不代表工具最终生成出的真实结果一定安全。
写操作必须在落库前做最后一道结果风险检查。
```

## 19. 工具输入输出类型系统起步

已经开始将 Action 从松散 `object / JSON` 结构升级为更稳定的输入输出模型：

- 新增 `SearchArticlesByCategoryInput`
- 新增 `SearchArticlesByCategoryOutput`
- 新增 `ArticleSearchResultItem`
- `SearchArticlesByCategory` 执行结果改为结构化 Output DTO
- `ArticleSortBy` 改为枚举表达
- 查询时先拿完整分类文章列表，再根据 `top` 截断展示，保留真实 `TotalCount`
- 新增 `IUserReadableOutput`
- 新增 `IAgentContentOutput`
- 新增 `IAgentArticleReferenceOutput`

同时，为避免 MemoryService 直接解析每个 Action 的 Output DTO，已加入统一记忆事实层：

- 新增 `AgentMemoryFact`
- 新增 `ArticleMemoryType`
- `AgentStepExecutionResult` 增加 `MemoryFacts`
- `SearchArticlesByCategory` 会产出 `ArticleQueried` 事实
- `SearchArticlesByCategory` 会产出 `ArticleSelected` 事实
- MemoryService 只消费 `ArticleSelected` 更新现有 `LastSelectedArticleId / LastSelectedArticleTitle`

当前策略：

```text
Action 自己决定哪些结果值得记忆。
MemoryService 不再猜测每个 Action Output DTO 的结构。
长期 Memory 拓宽暂缓到后续 Memory 阶段再做。
```

## 20. 十二个 Action DTO 标准化

当前已经完成 12 个既有 Action 的 Input / Output DTO 标准化：

- `SearchArticlesByCategory`
- `GetArticleContentById`
- `SummarizeContent`
- `AnswerQuestionFromContent`
- `CompareContents`
- `GetMyArticles`
- `UpdateArticleTitle`
- `GenerateContentRevision`
- `UpdateArticleContent`
- `GetAllCategories`
- `ExplainFailureWithSuggestions`
- `DeleteArticle`

已经验证的能力：

- 按分类查询点赞最高文章
- 获取文章正文并总结
- 直接总结用户输入的长文本
- 基于用户输入文本回答具体问题
- 基于文章正文回答具体问题
- 对比两篇文章内容
- 查询当前用户发布的文章，并支持 `top` 与 `sortBy`
- 根据上一步文章列表结果提取文章 ID，再执行标题修改
- 根据文章正文生成修订内容
- 修改文章正文需要确认，并在写库前做真实结果风险校验
- 查询全部文章分类
- 分类不存在时生成失败解释与补救建议
- 删除文章 Action 已 DTO 化，但仍保持 `Forbidden` 安全拒绝

当前已形成的 DTO 接口分层：

```text
IUserReadableOutput：输出可读文本，用于最终回答上下文。
IAgentContentOutput：输出正文内容，用于总结、问答、对比等后续步骤。
IAgentArticleReferenceOutput：输出主文章 ID，用于后续写操作或正文获取。
```

本阶段的关键收获：

```text
Action 的 Output 不只是给用户看的结果，也可能是后续 Action 的结构化输入来源。
所以 DTO 要同时服务“展示、传参、记忆”三个方向。
```

## 21. Action DTO 标准化收尾验证

已经完成最后一批 Action 的 DTO 化与测试：

- `GenerateContentRevision` 支持直接文本润色与基于前置正文生成修订内容
- `UpdateArticleContent` 使用结构化 Input / Output，并保留确认机制
- `GetAllCategories` 使用结构化 Output 返回分类列表
- `ExplainFailureWithSuggestions` 使用结构化 Output 返回失败解释和建议
- `DeleteArticle` 使用结构化 Input / Output，但风险等级仍为 `Forbidden`

已经验证：

- 用户直接贴长文本并要求总结 / 问答 / 润色时，不再误用旧文章内容
- 用户要求“把文章润色得更适合初学者阅读”时，能走获取正文、生成修订、确认写库流程
- 用户查询不存在分类时，能说明分类不存在，并列出可选分类
- 用户要求删除文章时，会被安全边界拒绝，不进入确认，不执行删除

当前 12 个 Action 的 DTO 标准化已经闭环：

```text
Action Input DTO -> Executor 强类型解析 -> Action Output DTO
-> FinalAnswer / Memory / 后续步骤复用
```

## 22. 既有 Action MemoryFacts 收尾

已经完成 12 个既有 Action 的第一阶段记忆事实设计与补齐：

- 列表型查询只记录 `ArticleMentioned`
- 列表只有 1 篇时才额外写入 `ArticleSelected`
- 单篇文章读取写入 `ArticleSelected`
- 文章标题 / 正文修改写入 `ArticleUpdated` 与 `ArticleSelected`
- 文章总结写入 `ArticleSummarized`
- 文章问答写入 `ArticleAnswered`
- 文章对比只记录被对比文章为 `ArticleMentioned`
- 直接粘贴文本总结 / 问答 / 润色不污染 `LastSelectedArticleId`
- `GetAllCategories`、`ExplainFailureWithSuggestions`、`DeleteArticle` 不产生文章记忆事实

已经验证：

- 查看 ID 为 9 的文章内容后，`LastSelectedArticleId` 正常更新
- “这篇文章讲了什么？” 能正确指代上一轮选中文章
- “刚才那篇文章现在标题是什么？” 能正确读取当前文章标题
- 对用户直接粘贴的文本进行润色，不会把上一轮文章错误选中
- 对比两篇文章时，不会把对比结果误当成新的唯一选中文章

本阶段形成的规则：

```text
Action Output 负责声明“我产生了哪些可记忆事实”。
MemoryService 只消费 MemoryFacts，不再猜每个 DTO 的内部结构。
```

## 23. 当前阶段定位

当前项目的 Agent 学习阶段大致处于：

```text
Agent 工程化中高级阶段
```

已经不只是实现“让模型回答问题”，而是在构建：

```text
可控、可观察、可评估、可审计、可复现、可迭代的 Agent 系统。
```
