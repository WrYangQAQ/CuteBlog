using CuteBlogSystem.AI.Planner;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    // 负责修复不合法的 AgentPlan，利用 AI 根据验证错误生成修正后的计划
    public class AgentPlanRepairService
    {
        private readonly IChatClient _chatClient;   // AI 客户端，用于生成修复后的计划
        private readonly ILogger<AgentPlanRepairService> _logger;   // 日志记录器

        public AgentPlanRepairService(
            IChatClient chatClient,
            ILogger<AgentPlanRepairService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        // 根据用户原始需求、无效的计划以及验证错误，请求 AI 修复并返回新的计划
        public async Task<AgentPlan> RepairPlanAsync(
            string userMessage,
            AgentPlan invalidPlan,
            AgentPlanValidationResult validationResult)
        {
            // 将无效计划序列化为 JSON，便于 AI 理解当前结构
            var invalidPlanJson = JsonSerializer.Serialize(
                invalidPlan,
                new JsonSerializerOptions { WriteIndented = true });

            // 将所有验证错误拼接为多行文本，供 AI 参考
            var errorsText = string.Join("\n", validationResult.Errors);

            // 构建系统提示和用户消息，引导 AI 按照规则修复
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客系统 Agent Plan Repairer。
                你的任务不是回答用户，而是修复一个不合法的 AgentPlan JSON。

                当前系统只允许以下 action：

                1. SearchArticlesByCategory
                   参数：
                   - categoryName：文章分类名称
                   - top：返回数量，必须是 1 到 10
                   - sortBy：只能是 Latest、MostLiked、MostViewed

                2. GetArticleContentById
                   参数：
                   - articleIdFromStep：必须引用之前某一步

                3. SummarizeContent
                   参数：
                   - content：用户直接提供的正文内容
                   - contentFromStep：从前置步骤读取正文

                4. AnswerQuestionFromContent
                   参数：
                   - contentFromStep：必须引用之前获取正文的步骤
                   - question：用户当前针对文章提出的具体问题

                5. CompareContents
                   参数：
                   - contentFromStepA：必须引用之前某一步
                   - contentFromStepB：必须引用之前某一步

                6. GetAllCategories
                   参数：无

                7. ExplainFailureWithSuggestions
                   参数：
                   - failureFromStep：失败步骤编号
                   - categoriesFromStep：分类列表来源步骤编号

                修复规则：
                - 只能输出 JSON。
                - 不要输出 Markdown。
                - 不要解释。
                - stepNumber 必须从 1 开始连续递增。
                - 后续步骤只能引用之前步骤，不能引用当前或未来步骤。
                - 不允许使用未列出的 action。
                - sortBy 只能使用 Latest、MostLiked、MostViewed。
                - 如果用户要求总结用户直接贴出的内容，直接使用 SummarizeContent(content=用户提供的正文)。
                - 如果用户要求总结博客系统中的某篇文章，才先查询/获取正文，再总结。
                - 如果用户要求对比，必须先查询两篇文章，再分别获取正文，再对比。
                - 只有用户明确要求总结、概括或询问“主要讲了什么”时，才使用 SummarizeContent。
                - 用户询问文章中的具体知识点时，必须使用 AnswerQuestionFromContent。
                - AnswerQuestionFromContent 前必须先使用 GetArticleContentById 获取正文。
                - question 必须保留用户当前的具体问题。
                - 执行 SummarizeContent 时参数 content 和 contentFromStep 二选一
                - 如果用户消息里已经贴出要总结的正文，必须直接使用 content，不要重新搜索文章
                - 如果用户要求总结某篇博客系统内文章，才先 GetArticleContentById，再 SummarizeContent(contentFromStep=...)
                """),

                new(ChatRole.User,
                $"""
                用户原始问题：
                {userMessage}

                当前不合法计划：
                {invalidPlanJson}

                校验错误：
                {errorsText}

                请修复为合法 AgentPlan JSON。
                """)
            };

            _logger.LogInformation("开始修复 AgentPlan，错误数量：{Count}", validationResult.Errors.Count);

            // 调用 AI 获取修复后的计划文本，并限制输出长度以避免过长响应
            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.PlanRepairMaxOutputTokens
                }
            );

            // 提取助手的回复（即修复后的 JSON）
            var repairedJson = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? string.Empty;

            // 去除可能的 Markdown 代码块标记，只保留 JSON
            repairedJson = ExtractJson(repairedJson);

            _logger.LogInformation("修复后的 AgentPlan：{PlanJson}", repairedJson);

            // 反序列化时忽略属性名大小写
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var repairedPlan = JsonSerializer.Deserialize<AgentPlan>(repairedJson, options);

            if (repairedPlan == null)
            {
                throw new InvalidOperationException("修复计划失败，无法反序列化为 AgentPlan。");
            }

            return repairedPlan;
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
    }
}