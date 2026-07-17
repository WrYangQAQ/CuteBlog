using CuteBlogSystem.AI.Planner;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    // 负责分析 Agent 执行失败的原因，通过 AI 生成可读的错误解释和建议
    public class AgentExecutionFailureAnalyzerService
    {
        private readonly IChatClient _chatClient;   // AI 客户端，用于生成失败分析
        private readonly ILogger<AgentExecutionFailureAnalyzerService> _logger;   // 日志记录器

        public AgentExecutionFailureAnalyzerService(
            IChatClient chatClient,
            ILogger<AgentExecutionFailureAnalyzerService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        // 根据原始计划和执行结果，返回失败原因分析文本
        public async Task<string> AnalyzeFailureAsync(
            AgentPlan plan,
            AgentPlanExecutionResult executionResult)
        {
            // 将计划和执行结果序列化为 JSON，便于 AI 理解
            var planJson = JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true });
            var executionJson = JsonSerializer.Serialize(executionResult, new JsonSerializerOptions { WriteIndented = true });

            // 构建系统提示和用户消息，要求 AI 分析失败原因
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个 Agent 执行失败分析助手。
                你的任务是根据 AgentPlan 和执行结果，解释为什么任务失败。

                要求：
                1. 不要编造不存在的信息。
                2. 先说明失败发生在哪一步。
                3. 再说明失败原因。
                4. 最后给用户一个可操作的建议。
                5. 回答要简洁、清楚。
                6. 如果失败涉及写入、修改、删除、清空、覆盖等操作，不要建议用户绕过安全检查。
                7. 不要建议直接调用 UpdateArticleContent、UpdateArticleTitle、DeleteArticle 等写操作接口。
                8. 不要建议跳过 GenerateContentRevision、跳过确认、跳过权限或参数校验。
                9. 如果失败原因与高风险参数、安全检查、清空正文、删除内容有关，应明确告诉用户：该请求因安全风险未被执行，并建议用户重新描述一个具体、安全、非清空式的修改目标。
                """),

                new(ChatRole.User,
                $"""
                AgentPlan：
                {planJson}

                执行结果：
                {executionJson}
                """)
            };

            _logger.LogInformation("开始分析 Agent 执行失败原因。");

            // 调用 AI 获取分析结果，并限制输出长度，防止过长回答
            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.FailureAnalysisMaxOutputTokens
                }
            );

            // 提取助手的回复文本，若为空则返回默认提示
            var analysis = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? "任务执行失败，但未能生成失败原因分析。";

            return analysis;
        }
    }
}