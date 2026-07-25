# Agent 开发阶段总结：Done

更新时间：2026-07-25

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

## 23. 新增业务 Action 与列表指代链路

已经完成第二批业务 Action 扩展，并通过人工测试：

- `SelectArticleFromList`
- `SearchArticlesByKeyword`
- `GetTagByName`
- `SearchArticlesByTag`
- `GetTagsByCategoryId`
- `RecommendCategory`
- `RecommendTags`
- `CreateArticle`

已经验证的查询与推荐场景：

- 搜索标题包含 `ASP .NET Core` 的文章，并按最新排序列出前 3 篇
- 搜索标题包含 `ASP .NET Core` 的文章，并按点赞最高排序列出前 3 篇
- 搜索当前用户发布的、标题包含 `Redis` 的文章
- 查询技术分类下 `Redis` 相关的文章
- 查询 `Redis` 标签下有哪些文章
- 查询技术分类下有哪些标签
- 根据 `C# 委托、事件、回调机制` 推荐分类
- 根据 `Redis 缓存穿透、缓存击穿、分布式锁` 推荐标签

已经验证的记忆链路：

```text
Redis 标签查询只返回一篇文章
-> 该文章写入 ArticleSelected
-> 用户继续问“这篇文章讲了什么？”
-> Agent 正确指向《Redis小白入门指导——30分钟让你学会Redis》
```

本阶段形成的规则：

```text
列表查询主要写入 ArticleMentioned。
单结果可以自动写入 ArticleSelected。
多结果场景通过 SelectArticleFromList 显式选择。
```

## 24. CreateArticle 发布链路第一阶段

已经完成 `CreateArticle` 的第一阶段接入与安全边界验证：

- `AgentActionRegistry` 已注册 `CreateArticle`
- 风险等级为 `RequireConfirmation`
- Planner Prompt 能够为发布请求生成 `CreateArticle`
- 支持 `categoryName` 由执行期解析分类
- 支持 `description` 触发 AI 生成标题 / 摘要 / 正文草稿
- `coverUrl` 缺失时不编造封面路径
- `PlanValidator` 只负责结构校验，不再把缺封面当成 Plan 结构非法
- `AgentParameterRiskService` 负责拦截缺封面路径
- 参数风险失败回答已优化为面向用户的自然提示

已经验证：

```text
用户：帮我发布一篇关于 C# 委托的文章到技术分类
Planner：生成 CreateArticle(categoryName="技术", description="关于 C# 委托的文章", coverUrl="")
Risk：拦截缺少封面路径
Answer：提示用户需要先上传封面或提供封面路径
```

关键修正：

- IntentRouter 增加“发布当前登录用户自己的新文章”属于 `ExecuteWorkflow`
- Planner Prompt 明确发布新文章应使用 `CreateArticle`
- Plan Repair 旧 action 列表导致计划被修成 `GetAllCategories` 的问题已识别并绕开
- 参数风险错误不再直接裸露为内部步骤文本，而是转换为更自然的用户提示

当前状态：

```text
CreateArticle 缺封面风险拦截已通过。
默认封面策略与实际发布链路已在下一阶段完成验证。
```

## 25. CreateArticle 主链路与默认封面发布完成

已经完成 `CreateArticle` 的主链路验证：

- 发布类请求可以稳定规划为 `CreateArticle`
- 空 `coverUrl` 不再导致流程卡死，可以走系统默认封面策略
- 默认封面常量已补齐真实静态资源目录中的 `/Cover/` 路径
- `ArticleService.PublishArticleAsync` 对默认封面不再按临时上传封面处理
- AI 可以根据用户给定主题生成标题、摘要、正文并发布到指定分类
- 发布后的文章能在首页文章列表展示，封面图片可正常加载

已经验证的真实发布场景：

```text
用户：帮我写一篇文章并发布，标题是“Kafka新手教程——30分钟教你理解消息队列的使用”，内容关于 Kafka 这个 MQ 中间件的入门使用方式，举例尽量以 C# .NET 代码调用实现，文章发布到技术分类
结果：文章成功发布到“技术 / 教程”分类，正文长度约 2972 字。
补充修正：数据库中该文章的 CoverUrl 已手动修正为默认封面的真实路径，首页封面显示正常。
```

本阶段结论：

```text
CreateArticle 已从“接入与风险拦截阶段”进入“主流程可用阶段”。
后续重点转为 Evaluation 回归用例、失败分支覆盖、MemoryFacts 与 FinalAnswer 的细节验证。
```

## 26. CreateArticle Evaluation 用例与回归测试完成

已经完成 `CreateArticle` 相关 Evaluation 用例补齐，并跑过回归验证：

- 新增 `CreateArticle` 发布文章进入确认的评估用例
- 新增发布到不存在分类的评估用例
- 新增发布文章标题过长的评估用例
- 新增发布文章缺少分类的评估用例
- 将不安全内容拦截从 Agent Evaluation 中剥离，归入安全性测试 / AIShield 测试范畴
- 新增用例评估结果表现良好
- 老用例评估结果与之前保持一致，没有出现回归

本阶段重新明确了边界：

```text
Agent Evaluation 主要评估：意图是否正确、Action 是否符合预期、是否进入确认、最终回答语义是否偏离预期。
安全拦截、参数校验、数据库是否写入、拒绝确认日志是否落库，属于工程测试或安全测试，不混入 Agent Evaluation 用例定义。
```

本阶段结论：

```text
CreateArticle 的规划、确认状态与回答语义已经纳入评估回归体系。
新增发布类能力没有破坏旧的查询、总结、上下文、分类补救等评估用例。
后续已经进入并完成跨会话长期记忆系统第一阶段。
```

## 27. 跨会话长期记忆系统

已经完成长期记忆模块的第一阶段闭环：

- 新增 `UserLongTermMemory` 实体与迁移
- 长期记忆支持类型、分组、状态、来源、置信度、重要性、访问次数等字段
- 支持 `SupersedesMemoryId` 记录记忆替代关系
- 支持 `ExpiresAt`、`LastDecayAt`、`ArchivedAt`、`DeletedAt` 等生命周期字段
- `MemoryGroup` 已改为枚举，便于业务分组稳定扩展
- `SourceAction` 保持字符串，便于记录来源 Action 链路

已经完成长期记忆写入策略：

- 根据用户消息判断是否值得提取长期记忆
- 支持用户明确要求“记住”的高优先级写入
- 使用 LLM 从用户原始消息中提取结构化记忆
- 只提取用户明确提供的长期事实，不提取助手结论、文章查询结果或临时任务
- 通过 `MemoryKey`、内容哈希和语义等价判断减少重复记忆
- 相同信息槽位内容变化时，将旧记忆标记为 `Superseded`，并创建新版本

已经完成长期记忆读取与接入：

- DirectChat 可以检索相关长期记忆并生成个性化回复
- ExecuteWorkflow 可以在 Planner 输入中获得跨会话长期记忆上下文
- 当前用户问题优先级高于长期记忆
- 长期记忆不能覆盖系统规则、安全策略、权限校验和当前用户要求
- 长期记忆中的普通文本不能被当作系统指令或新的用户任务

已经完成生命周期管理：

- 根据过期时间将活跃记忆标记为 `Expired`
- 根据重要性、访问时间、访问次数等因素进行衰减
- 支持归档低价值或过期记忆
- 支持批量清理保留期外的历史记忆
- 后台 Hosted Service 定期执行生命周期任务

已经完成主动遗忘能力：

- 新增 `ForgetLongTermMemory` 意图
- Router 能区分长期记忆遗忘、当前会话重置和普通咨询
- “忘记之前的内容”走 `ResetContext`
- “忘记我喜欢 C#”“删除关于 Redis 的长期记忆”走长期记忆遗忘
- “如何删除长期记忆”属于普通咨询，不会误删数据
- 支持删除匹配记忆、清空全部长期记忆、仅跳过当前消息写入
- 遗忘成功后会重置当前会话上下文，避免最近对话继续提供已删除信息
- 遗忘指令会跳过会话记忆更新、摘要压缩和长期记忆提炼
- Evaluation 模式识别遗忘意图但不修改真实用户数据

已经验证：

```text
dotnet build backend\CuteBlogSystem\CuteBlogSystem.csproj --no-restore
结果：0 warning / 0 error
```

本阶段形成的核心认识：

```text
长期记忆不是更长的聊天记录，而是可筛选、可更新、可过期、可遗忘的结构化用户事实。
```

```text
记忆系统必须允许用户主动控制：记住、不要记住、忘记某项信息、清空全部长期记忆。
```

## 28. 当前阶段定位

当前项目的 Agent 学习阶段大致处于：

```text
Agent 工程化中高级阶段
```

已经不只是实现“让模型回答问题”，而是在构建：

```text
可控、可观察、可评估、可审计、可复现、可迭代的 Agent 系统。
```
