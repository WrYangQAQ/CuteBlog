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

                3. SummarizeContent
                   用于总结文章正文。
                   参数：
                   - contentFromStep：表示从哪一步的结果中取正文内容

                4. CompareContents
                   用于对比两篇文章正文。
                   参数：
                   - contentFromStepA：第一篇正文来自哪一步
                   - contentFromStepB：第二篇正文来自哪一步

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
                - 如果用户说“那一篇、第一篇、一篇”，top 使用 1。
                - 如果用户要求总结文章，必须先查询文章，再获取正文，再总结正文。
                - 如果后续步骤需要使用前面步骤的文章ID，用 articleIdFromStep 表示。
                - 如果后续步骤需要使用前面步骤的正文，用 contentFromStep 表示。
                """),

                new(ChatRole.User, userMessage)
            };

            _logger.LogInformation("Planner 开始生成计划，用户问题：{Message}", userMessage);

            // 调用 AI 获取原始响应
            var response = await _chatClient.GetResponseAsync(messages);

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