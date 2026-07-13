using CuteBlogSystem.AI.Planner;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    public class AiPlannerService
    {
        private readonly IChatClient _chatClient;   // AI聊天客户端，用于生成计划
        private readonly ILogger<AiPlannerService> _logger;   // 日志记录器

        public AiPlannerService(
            IChatClient chatClient,
            ILogger<AiPlannerService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        public async Task<AgentPlan> CreatePlanAsync(string userMessage)
        {
            // 校验用户输入不能为空
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                throw new ArgumentException("用户消息不能为空", nameof(userMessage));
            }

            // 构建对话消息：系统指令定义了可用的动作和输出格式，用户消息即原始请求
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客系统 Agent Planner。
                你的任务不是直接回答用户，而是把用户目标拆解成可执行的 JSON 计划。

                当前系统允许的 action 只有以下几种：

                1. SearchArticlesByCategory
                   用于按分类查询文章。
                   参数：
                   - categoryName：文章分类名称，例如 技术、生活、随笔
                   - top：返回数量
                   - sortBy：排序方式，只能是 Latest、MostLiked、MostViewed

                2. GetArticleContentById
                   用于根据文章ID获取文章正文。
                   参数：
                   - articleIdFromStep：表示从哪一步的结果中取文章ID
                   - 当使用 articleId 直接指定文章时，不要再输出 articleIdFromStep。
                   - 当使用 articleIdFromStep 引用前置步骤时，不要再输出 articleId。
                   - articleId 和 articleIdFromStep 二选一，不要同时输出。

                3. SummarizeContent
                   用于总结文章正文。
                   参数：
                   - contentFromStep：表示从哪一步的结果中取正文内容

                4. AnswerQuestionFromContent
                   用于根据文章正文回答用户提出的具体问题。
                   参数：
                   - contentFromStep：正文来自哪一步
                   - question：用户针对文章提出的具体问题

                5. CompareContents
                   用于对比两篇文章正文。
                   参数：
                   - contentFromStepA：第一篇正文来自哪一步
                   - contentFromStepB：第二篇正文来自哪一步

                6. GetAllCategories
                   用于获取系统中所有文章分类。
                   当用户指定的分类可能不存在、分类名称不确定、或者需要给用户提供可选分类建议时使用。
                   参数：无

                7. ExplainFailureWithSuggestions
                   用于根据查询失败、查询结果为空、分类不匹配等情况，以及当前可用分类，生成面向用户的解释和建议。
                   参数：
                   - failureFromStep：查询失败或查询结果为空的步骤编号
                   - categoriesFromStep：分类列表来源步骤编号
                   - requestedCategoryName：用户原始请求中的分类名称
                                
                8. GetMyArticles
                   查询当前登录用户自己的文章列表。
                   参数：
                   - page：可选，默认 1
                   - pageSize：可选，默认 10，最大 20
                   注意：
                   - 不需要 userId，系统会使用当前登录用户身份。
                   - 当用户说“我的文章”、“我发布过哪些文章”、“列出我的博客文章”时使用。

                9. UpdateArticleTitle
                   用于修改当前登录用户自己文章的标题。
                   参数：
                   - articleId：必填，整数，文章ID。
                   - newTitle：必填，字符串，新标题，不能超过30个字符。
                   注意：
                   - 不需要 userId，系统会使用当前登录用户身份判断权限。
                   - 只有当用户明确表达“修改标题、改标题、把标题改成...”时才使用。
                   - 如果用户只是让你“帮我想标题、优化标题、生成标题建议”，不要使用该动作，应直接聊天回答。
                   - 如果用户没有提供文章ID，但上下文记忆中有上一轮选中文章，可以使用该文章ID。
                   - 如果用户没有提供文章ID，也没有可用上下文指代，不要编造 articleId，应让用户指定文章。
                   - 该动作属于修改操作，执行前系统会要求用户确认。

                10. GenerateContentRevision
                    用于根据用户指令生成文章的修订版本（不直接写入数据库）。
                    参数：
                    - contentFromStep：必填（或 originalContent），指定原文来源步骤（通常来自 GetArticleContentById）。
                    - instruction：可选，修改指令，例如“将语气改为更正式”、“精简到500字”、“增加案例说明”等。
                    注意：
                    - 该动作只生成新内容，不修改数据库，新内容会存入步骤结果供后续使用。

                11. UpdateArticleContent
                    用于将新内容写入指定文章（修改数据库）。
                    参数：
                    - articleId 或 articleIdFromStep：指定要修改的文章ID。
                    - newContent 或 newContentFromStep：指定新内容（通常来自 GenerateContentRevision 的结果）。
                    注意：
                    - 该动作属于写操作，执行前系统会要求用户确认。
                                
                12. DeleteArticle
                    用于识别用户明确要求删除文章的意图。
                    参数：
                    - articleId 或 articleIdFromStep：指定要删除的文章ID。
                    注意：
                    - 当前系统策略禁止 Agent 执行删除文章操作。
                    - 该动作只用于让风险控制层识别并拒绝删除请求，不会真正执行删除。
                    - 只有当用户明确表达“删除文章”、“删掉这篇”、“移除这篇文章”等意图时才使用。
                    - 如果用户没有提供文章ID，且上下文中没有可指代的文章，不要编造 articleId，应要求用户明确指定。


                你必须只输出 JSON，不要输出 Markdown，不要输出解释文字。
                JSON 格式必须严格如下：

                {
                  "goal": "用户目标",
                  "steps": [
                    {
                      "stepNumber": 1,
                      "action": "SearchArticlesByCategory",
                      "description": "步骤说明",
                      "parameters": {
                        "categoryName": "技术",
                        "top": 1,
                        "sortBy": "MostLiked"
                      }
                    }
                  ]
                }

                规则：
                - 如果用户要求“最新文章”，sortBy 使用 Latest。
                - 如果用户要求“点赞最高、点赞最多、最受欢迎”，sortBy 使用 MostLiked。
                - 如果用户要求“浏览量最高、浏览最多、访问量最高”，sortBy 使用 MostViewed。
                - 如果用户指定的分类名称明显可能不存在、不确定，或者用户输入中包含“不存在分类”“不存在的分类”“没有的分类”等表达，
                  不要只生成 SearchArticlesByCategory。
                  必须生成三步计划：
                  第一步使用 SearchArticlesByCategory，按用户指定分类查询文章。
                  第二步使用 GetAllCategories，获取当前系统可用分类。
                  第三步使用 ExplainFailureWithSuggestions，failureFromStep 引用第一步，categoriesFromStep 引用第二步。
                - 对于这种分类不存在或分类不确定的场景，即使用户要求“点赞最高”“最新”“浏览量最高”，也仍然需要执行 GetAllCategories 来提供补救建议。
                - 如果用户说“那一篇、第一篇、一篇”，top 使用 1。
                - 如果用户要求总结文章，必须先查询文章，再获取正文，再总结正文。
                - 如果后续步骤需要使用前面步骤的文章ID，用 articleIdFromStep 表示。
                - 如果后续步骤需要使用前面步骤的正文，用 contentFromStep 表示。
                - 如果用户要求“对比”两篇文章，必须先分别查询两篇文章，再分别获取两篇文章正文，最后使用 CompareContents。
                - 如果用户要求对比“点赞最高”和“浏览量最高”的文章：
                  第一步使用 SearchArticlesByCategory，sortBy = MostLiked，top = 1。
                  第二步使用 SearchArticlesByCategory，sortBy = MostViewed，top = 1。
                  第三步获取第一步文章正文。
                  第四步获取第二步文章正文。
                  第五步对比第三步和第四步正文。
                - 只有用户明确要求“总结、概括、主要讲了什么”时，才使用 SummarizeContent。
                - 用户针对文章询问具体内容时，例如“有没有介绍变量”“var 是怎么解释的”
                 “介绍了哪些流程控制语句”，必须使用 AnswerQuestionFromContent。
                - AnswerQuestionFromContent 前必须先通过 GetArticleContentById 获取文章正文。
                - question 必须填写用户当前提出的具体问题，不要填写完整上下文或历史摘要。
                - 当用户明确要求修改文章标题时，使用 UpdateArticleTitle。
                - UpdateArticleTitle 的参数必须使用 newTitle，不要使用 title。
                - 如果用户说“把这篇文章标题改成 xxx”，并且上下文中存在上一轮选中的文章ID，可以直接使用该 articleId。
                - 如果无法确定要修改哪篇文章，不要生成 UpdateArticleTitle，应该先让用户提供文章ID或明确文章。
                - 如果用户只是请求“帮我起标题 / 优化标题 / 给标题建议”，不要修改数据库，不要使用 UpdateArticleTitle。
                - 当用户要求“修改文章内容”、“重写文章”、“润色文章”、“把文章改得更好”等，必须使用 GenerateContentRevision 生成新内容，然后再用 UpdateArticleContent 保存。
                - 如果用户直接要求“把文章改成 xxx”，但未明确具体修改指令，仍应先生成修订建议（GenerateContentRevision），再执行更新。
                - 生成修订内容时，必须优先通过 GetArticleContentById 获取原文，然后将 contentFromStep 指向该步骤。
                - UpdateArticleContent 的 newContentFromStep 必须指向 GenerateContentRevision 的步骤结果。
                - 例如，用户说“帮我把第一篇文章改得更通俗易懂”，计划应为：
                  1. GetArticleContentById (articleId = 1)
                  2. GenerateContentRevision (contentFromStep=1, instruction="改得更通俗易懂")
                  3. UpdateArticleContent (articleId=1, newContentFromStep=2)
                - 当用户说“删除我最近写的那篇”或“把 id 为 5 的文章删掉”时，使用 DeleteArticle。
                - 当计划中包含 DeleteArticle 时，系统会在风险控制阶段拒绝执行，不会进入真正删除逻辑。
                - 如果用户只是说“我不喜欢这篇文章”但没有明确要求删除，不要使用 DeleteArticle，应直接聊天回复。
                - DeleteArticle 必须配合明确的文章ID或上一步的查询结果（通过 articleIdFromStep）。
                - 示例流程：
                  1. GetMyArticles（获取文章列表供用户参考）
                  2. DeleteArticle（articleIdFromStep = 1，删除列表中的第一篇）

                意图区分示例：
                - “这篇文章主要讲了什么？” → SummarizeContent
                - “总结一下这篇文章” → SummarizeContent
                - “文章中如何解释 var？” → AnswerQuestionFromContent
                - “有没有介绍变量？” → AnswerQuestionFromContent
                - “介绍了哪些流程控制语句？” → AnswerQuestionFromContent
                - “帮我查找不存在分类下点赞最高的一篇文章”
                  → Step 1: SearchArticlesByCategory，categoryName = "不存在分类"，sortBy = MostLiked，top = 1
                  → Step 2: GetAllCategories
                  → Step 3: ExplainFailureWithSuggestions，failureFromStep = 1，categoriesFromStep = 2，requestedCategoryName = "不存在分类"

                - “帮我查找火星农业技术分类下最新的一篇文章”
                  → Step 1: SearchArticlesByCategory，categoryName = "火星农业技术"，sortBy = Latest，top = 1
                  → Step 2: GetAllCategories
                  → Step 3: ExplainFailureWithSuggestions，failureFromStep = 1，categoriesFromStep = 2

                禁止把具体知识点问答转换成全文总结。
                只要问题能够针对文章中的某个局部内容作答，就必须使用 AnswerQuestionFromContent。
                """),

                new(ChatRole.User, userMessage)
            };

            _logger.LogInformation("Planner 开始生成计划，用户问题：{Message}", userMessage);

            // 调用 AI 获取原始响应，并限制最大 token 数量以控制输出长度
            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.PlannerMaxOutputTokens
                }
            );

            // 提取助手的回复文本
            var planJson = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? string.Empty;

            // 去除可能的 Markdown 代码块标记，提取纯 JSON
            planJson = ExtractJson(planJson);

            _logger.LogInformation("Planner 生成的原始计划：{PlanJson}", planJson);

            // 反序列化时忽略属性名大小写差异
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var plan = JsonSerializer.Deserialize<AgentPlan>(planJson, options);

            // 反序列化失败则抛出异常
            if (plan == null)
            {
                throw new InvalidOperationException("Planner 生成计划失败，无法反序列化为 AgentPlan。");
            }

            return plan;
        }

        // 从可能包含 Markdown 标记的文本中提取出纯 JSON 字符串
        private static string ExtractJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim();

            // 移除开头的 ```json 或 ``` 以及结尾的 ```
            if (text.StartsWith("```json"))
            {
                text = text.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (text.StartsWith("```"))
            {
                text = text.Replace("```", "").Trim();
            }

            // 找到第一个 '{' 和最后一个 '}'，截取中间的 JSON 内容
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                return text[start..(end + 1)];
            }

            return text;
        }
    }
}