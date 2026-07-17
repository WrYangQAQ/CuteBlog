using CuteBlogSystem.AI.Planner;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    // 负责在原始计划执行失败后，生成一个安全的补救计划
    public class AgentReplannerService
    {
        private readonly IChatClient _chatClient;   // AI 客户端，用于生成补救计划
        private readonly ILogger<AgentReplannerService> _logger;   // 日志记录器

        public AgentReplannerService(
            IChatClient chatClient,
            ILogger<AgentReplannerService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        // 根据用户需求、原始计划和失败执行结果，生成一个新的补救计划
        public async Task<AgentPlan> CreateRecoveryPlanAsync(
            string userMessage,
            AgentPlan originalPlan,
            AgentPlanExecutionResult failedExecutionResult)
        {
            // 序列化原始计划和失败结果，供 AI 分析
            var originalPlanJson = JsonSerializer.Serialize(originalPlan, new JsonSerializerOptions { WriteIndented = true });
            var failedExecutionJson = JsonSerializer.Serialize(failedExecutionResult, new JsonSerializerOptions { WriteIndented = true });

            // 确定原始计划中第一个失败的步骤编号，作为补救计划生成的参考
            var firstFailedStepNumber = failedExecutionResult.StepResults
                .Where(r => !r.Success)
                .Select(r => r.StepNumber)
                .FirstOrDefault();

            if (firstFailedStepNumber <= 0)
            {
                firstFailedStepNumber = failedExecutionResult.StepResults
                    .LastOrDefault()?.StepNumber ?? 1;
            }

            if (!CanRecoverWithReadOnlySuggestions(failedExecutionResult))
            {
                _logger.LogInformation(
                    "当前失败不适合通过分类建议补救，跳过 Replanner。FirstFailedStep = {StepNumber}",
                    firstFailedStepNumber);

                return new AgentPlan
                {
                    Goal = userMessage,
                    Steps = new List<AgentPlanStep>()
                };
            }

            // 构建系统指令和用户消息，引导 AI 生成符合规则的补救计划
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客系统 Agent Replanner。
                原始计划执行失败后，你需要生成一个安全的补救计划。

                **【重要】JSON 顶层结构约束（必须严格遵守）**
                - 根对象必须包含且仅包含两个字段：`Goal` 和 `Steps`。
                - **禁止使用 `plan` 作为根键名**（例如 `{"plan": [...]}` 是非法格式）。
                - `Goal` 字段必须是非空字符串，描述本次补救计划的目标（例如："为查询失败生成恢复建议"）。
                - `Steps` 字段必须是非空数组，至少包含一个步骤对象。

                **正确示例（请严格模仿此格式）**
                {
                  "Goal": "用户查询「火星农业」分类失败，生成补救建议",
                  "Steps": [
                    {
                      "StepNumber": 1,
                      "Action": "GetAllCategories",
                      "Description": "获取当前系统所有可用分类",
                      "Parameters": {}
                    },
                    {
                      "StepNumber": 2,
                      "Action": "ExplainFailureWithSuggestions",
                      "Description": "根据失败步骤和分类列表生成友好建议",
                      "Parameters": {
                        "failureFromStep": 1,
                        "categoriesFromStep": 1,
                        "requestedCategoryName": "火星农业"
                      }
                    }
                  ]
                }

                ---

                当前只允许生成以下 action：
                1. GetAllCategories
                   用于获取当前系统中所有文章分类。
                   参数：{}

                2. GetMyArticles
                   用于获取当前用户自己的文章列表。
                   适用场景：
                   - 原始失败原因是文章 ID 不存在
                   - 文章 ID 不明确
                   - 无法确定用户指的是哪篇文章
                   参数：
                   - top：可选，默认 10，最大 20
                   - sortBy：可选，只能是 Latest、MostLiked、MostViewed

                3. SearchArticlesByCategory
                   用于按分类重新查询文章列表。
                   适用场景：
                   - 分类可能存在，但原始查询结果为空
                   - 需要补充同分类文章列表作为解释上下文
                   参数：
                   - categoryName：分类名称（必填）
                   - sortBy：排序方式，可选 Latest、MostLiked、MostViewed
                   - top：返回数量，建议 5

                4. GetArticleContentById
                   用于根据文章 ID 获取文章正文。
                   适用场景：
                   - 原始失败发生在总结、问答、正文处理阶段
                   - 原始计划或失败结果中已经有明确可用的 articleId
                   参数：
                   - articleId：文章 ID（必填）

                5. ExplainFailureWithSuggestions
                   用于根据原始失败步骤和补救步骤获取到的上下文，生成用户友好的失败说明和建议。
                   参数：
                   - failureFromStep：原始失败步骤编号（必填）
                   - categoriesFromStep：分类列表来源步骤编号（可选）
                   - articlesFromStep：当前用户文章列表来源步骤编号（可选）
                   - searchResultsFromStep：补救查询结果来源步骤编号（可选）
                   - contentFromStep：文章正文来源步骤编号（可选）
                   - requestedCategoryName：用户原始请求中的分类名称（可选）

                **执行规则**
                - 只输出纯 JSON，不要输出 Markdown 代码块（如 ```json），不要输出任何解释文字。
                - 如果原始失败原因是分类没有文章或无法提取文章ID，则生成的补救计划必须包含两步：
                  Step 1: GetAllCategories（无参数）
                  Step 2: ExplainFailureWithSuggestions（引用 Step 1 的结果）
                - ExplainFailureWithSuggestions 的 failureFromStep 必须引用 failedExecutionResult.stepResults 中 success = false 的步骤编号。
                - 如果某一步 success = true 但 data 为空，它不是 failureFromStep，不要引用它。
                - failureFromStep 指的是“真正执行失败的那一步”，而不是“失败原因来源步骤”。
                - categoriesFromStep 必须引用补救计划中 GetAllCategories 的步骤编号（通常为 1）。
                - 不要自动把用户原来的分类替换成其他分类去继续查询，只需要如实反映失败并提供现有分类列表。
                - 如果失败原因是分类不存在、分类名称不明确、分类下无文章，优先使用 GetAllCategories，然后 ExplainFailureWithSuggestions。
                - 如果失败原因是文章 ID 不存在、文章 ID 不明确、无法确定用户指的是哪篇文章，优先使用 GetMyArticles，然后 ExplainFailureWithSuggestions。
                - 如果失败原因是查询结果为空但分类可能存在，可以使用 SearchArticlesByCategory 再补充查询上下文。
                - 如果失败原因发生在总结、问答、正文处理阶段，并且已有明确 articleId，可以使用 GetArticleContentById 获取正文后再 ExplainFailureWithSuggestions。
                - 禁止生成任何写操作、修改操作、删除操作。
                - 禁止使用 GenerateContentRevision、UpdateArticleTitle、UpdateArticleContent、DeleteArticle。
                """),

                new(ChatRole.User,
                $$"""
                用户原始问题：
                {userMessage}

                原始计划：
                {originalPlanJson}

                失败执行结果：
                {failedExecutionJson}

                原始执行流程中，第一个真正失败的步骤编号是：
                {firstFailedStepNumber}

                重要规则：
                - ExplainFailureWithSuggestions 的 failureFromStep 必须等于 {firstFailedStepNumber}
                - failureFromStep 必须引用 failedExecutionResult.stepResults 中 success = false 的步骤
                - 不要把 failureFromStep 写成返回空数据但 success = true 的步骤
                - ExplainFailureWithSuggestions 可以根据补救计划实际步骤选择 categoriesFromStep、articlesFromStep、searchResultsFromStep 或 contentFromStep。
                - 如果补救计划中使用了 GetAllCategories，则 categoriesFromStep 应该引用该步骤。
                - 如果补救计划中使用了 GetMyArticles，则 articlesFromStep 应该引用该步骤。
                - 如果补救计划中使用了 SearchArticlesByCategory，则 searchResultsFromStep 应该引用该步骤。
                - 如果补救计划中使用了 GetArticleContentById，则 contentFromStep 应该引用该步骤。

                请生成补救计划 JSON。
                """)
            };

            _logger.LogInformation("开始生成补救计划。");

            // 调用 AI 获取响应
            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.ReplannerMaxOutputTokens
                }
            );

            // 提取助手回复的文本（即补救计划 JSON）
            var recoveryPlanJson = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? string.Empty;

            // 去除可能的 Markdown 代码块标记，保留纯 JSON
            recoveryPlanJson = ExtractJson(recoveryPlanJson);

            _logger.LogInformation("补救计划 JSON：{PlanJson}", recoveryPlanJson);

            // 反序列化为 AgentPlan 对象（忽略属性名大小写）
            var plan = JsonSerializer.Deserialize<AgentPlan>(
                recoveryPlanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (plan == null || plan.Steps.Count == 0)
            {
                _logger.LogWarning(
                    "AI 生成的补救计划为空，改用确定性兜底补救计划。原始 JSON：{PlanJson}",
                    recoveryPlanJson);

                return CreateDefaultRecoveryPlan(userMessage, firstFailedStepNumber, failedExecutionResult);
            }

            if (string.IsNullOrWhiteSpace(plan.Goal))
            {
                plan.Goal = userMessage;
            }

            return plan;
        }

        // 生成默认的补救计划：获取所有分类 + 根据失败步骤生成建议
        private static AgentPlan CreateDefaultRecoveryPlan(
            string userMessage, 
            int failedStepNumber,
            AgentPlanExecutionResult failedExecutionResult)
        {
            var failedMessage = failedExecutionResult.StepResults
                .Where(r => !r.Success)
                .Select(r => r.Message)
                .FirstOrDefault() ?? string.Empty;

            if (IsArticleLookupFailure(failedMessage))
            {
                return CreateMyArticlesRecoveryPlan(userMessage, failedStepNumber);
            }

            return CreateCategoryRecoveryPlan(userMessage, failedStepNumber);
        }

        // 从可能包含 Markdown 标记的文本中提取纯 JSON 字符串
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

        // 判断执行失败是否可以通过“分类建议”进行恢复（如分类不存在、无文章、提取ID失败等）
        private static bool CanRecoverWithReadOnlySuggestions(AgentPlanExecutionResult failedExecutionResult)
        {
            // 从失败的步骤中提取第一条错误消息
            var failedMessage = failedExecutionResult.StepResults
                .Where(r => !r.Success)
                .Select(r => r.Message)
                .FirstOrDefault() ?? string.Empty;

            // 定义可通过分类建议恢复的错误关键词
            var recoverableKeywords = new[]
            {
                "没有找到",
                "未找到",
                "分类不存在",
                "分类名称",
                "无法获取文章ID",
                "无法提取文章ID",
                "没有可用文章",
                "文章列表为空",

                "文章ID",
                "文章 ID",
                "文章不存在",
                "无法确定文章",
                "无法获取文章正文",
                "无法读取文章",
                "查询结果为空",
                "没有匹配文章"
            };

            // 如果错误消息匹配任一关键词，返回 true
            return recoverableKeywords.Any(keyword =>
                failedMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        // 判断step失败是否为文章查询或掐他文章相关操作失败
        private static bool IsArticleLookupFailure(string message)
        {
            var keywords = new[]
            {
                "文章ID",
                "文章 ID",
                "文章不存在",
                "无法确定文章",
                "无法获取文章正文",
                "无法读取文章"
            };

            return keywords.Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        // 构建文章查询或操作失败的恢复plan
        private static AgentPlan CreateMyArticlesRecoveryPlan(string userMessage, int failedStepNumber)
        {
            return new AgentPlan
            {
                Goal = $"原任务失败后，查询当前用户文章列表并生成补救建议：{userMessage}",
                Steps = new List<AgentPlanStep>
                {
                    new()
                    {
                        StepNumber = 1,
                        Action = AgentActionRegistry.GetMyArticles,
                        Description = "获取当前用户的文章列表，用于帮助用户确认可操作的文章",
                        Parameters = new Dictionary<string, object>
                        {
                            { "top", 10 },
                            { "sortBy", "Latest" }
                        }
                    },
                    new()
                    {
                        StepNumber = 2,
                        Action = AgentActionRegistry.ExplainFailureWithSuggestions,
                        Description = "根据原始失败步骤和当前用户文章列表生成补救建议",
                        Parameters = new Dictionary<string, object>
                        {
                            { "failureFromStep", failedStepNumber },
                            { "articlesFromStep", 1 }
                        }
                    }
                }
            };
        }

        // 构建分类查询失败的恢复plan
        private static AgentPlan CreateCategoryRecoveryPlan(string userMessage, int failedStepNumber)
        {
            return new AgentPlan
            {
                Goal = $"原任务失败后，查询系统分类并生成补救建议：{userMessage}",
                Steps = new List<AgentPlanStep>
                {
                    new()
                    {
                        StepNumber = 1,
                        Action = AgentActionRegistry.GetAllCategories,
                        Description = "获取系统中所有可用文章分类",
                        Parameters = new Dictionary<string, object>()
                    },
                    new()
                    {
                        StepNumber = 2,
                        Action = AgentActionRegistry.ExplainFailureWithSuggestions,
                        Description = "根据原始失败步骤和可用分类生成补救建议",
                        Parameters = new Dictionary<string, object>
                        {
                            { "failureFromStep", failedStepNumber },
                            { "categoriesFromStep", 1 }
                        }
                    }
                }
            };
        }
    }
}
