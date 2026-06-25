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

            // 构建系统指令和用户消息，引导 AI 生成符合规则的补救计划
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客系统 Agent Replanner。
                原始计划执行失败后，你需要生成一个安全的补救计划。

                当前只允许生成以下 action：
                1. GetAllCategories
                   用于获取当前系统中所有文章分类。
                   参数：{}

                2. ExplainFailureWithSuggestions
                   用于根据失败步骤和分类列表生成用户友好的建议。
                   参数：
                   - failureFromStep：失败步骤编号
                   - categoriesFromStep：分类列表来源步骤编号

                规则：
                - 只输出 JSON。
                - 不要输出 Markdown。
                - 不要解释。
                - 不要自动把用户原来的分类替换成其他分类继续查询。
                - 如果原始失败原因是分类没有文章或无法提取文章ID，则生成：
                  Step 1: GetAllCategories
                  Step 2: ExplainFailureWithSuggestions
                - ExplainFailureWithSuggestions 的 failureFromStep 必须引用 failedExecutionResult.stepResults 中 success = false 的步骤编号。
                - 如果某一步 success = true 但 data 为空，它不是 failureFromStep。
                - failureFromStep 不是“失败原因来源步骤”，而是“真正执行失败的步骤”。
                - categoriesFromStep 必须引用补救计划中 GetAllCategories 的步骤编号，通常是 1。
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
                - categoriesFromStep 应该引用补救计划中的 GetAllCategories 步骤，一般是 1

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

                return CreateDefaultRecoveryPlan(userMessage, firstFailedStepNumber);
            }

            if (string.IsNullOrWhiteSpace(plan.Goal))
            {
                plan.Goal = userMessage;
            }

            return plan;
        }

        // 当前补救流程是固定的：获取所有分类，再基于原始失败步骤生成建议。
        // 这类确定性流程由代码兜底，比完全依赖模型稳定。
        private static AgentPlan CreateDefaultRecoveryPlan(
            string userMessage,
            int failedStepNumber)
        {
            return new AgentPlan
            {
                Goal = userMessage,
                Steps = new List<AgentPlanStep>
                {
                    new()
                    {
                        StepNumber = 1,
                        Action = AgentActionRegistry.GetAllCategories,
                        Description = "获取系统中所有文章分类",
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
