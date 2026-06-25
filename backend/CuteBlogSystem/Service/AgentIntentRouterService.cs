using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    public class AgentIntentRouterService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<AgentIntentRouterService> _logger;

        public AgentIntentRouterService(
            IChatClient chatClient, 
            ILogger<AgentIntentRouterService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        // 主入口：对用户消息进行意图识别路由：本地规则优先，未命中则调用 AI 模型，异常时降级为默认工作流
        public async Task<AgentIntentResult> RouteAsync(string userMessage)
        {
            // 空消息直接返回不支持意图
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return CreateResult(
                    AgentIntentType.Unsupported,
                    1,
                    "用户输入为空");
            }

            // 明确、低风险的命令优先走本地规则。
            var localResult = TryRouteLocally(userMessage);

            // 本地规则匹配成功则直接返回，无需调用模型
            if (localResult != null)
            {
                return localResult;
            }

            // 本地规则无法匹配，尝试用 AI 模型识别意图
            try
            {
                return await RouteWithModelAsync(userMessage);
            }
            catch (Exception ex)
            {
                // 模型调用异常时降级为默认工作流，保证可用性
                _logger.LogWarning(
                    ex,
                    "Agent 意图识别失败，降级为执行工作流。用户消息：{Message}",
                    userMessage);

                // 路由器故障不能阻断主要 Agent 功能。
                return CreateResult(
                    AgentIntentType.ExecuteWorkflow,
                    0,
                    "意图识别异常，降级为工作流");
            }
        }

        // 在路由type为DirectChat的时候，进行直接回复
        public async Task<string> GenerateDirectChatResponseAsync(string userMessage)
        {
            // 创建模型消息
            var messages = new List<ChatMessage>
            {
                new ChatMessage
                (
                    ChatRole.System,
                    """
                    你是 Sharky 博客系统的友好助手。

                    当前用户只是在进行简单交流，不需要调用任何工具。

                    要求：
                    1. 简洁自然地回应。
                    2. 不要生成执行计划。
                    3. 不要声称已经查询、修改或删除了任何数据。
                    4. 可以简短提示用户继续提出博客相关任务。
                    5. 回答控制在100字以内。
                    """
                ),
                new ChatMessage
                (
                    ChatRole.User,
                    userMessage
                )
            };

            // 等待模型响应
            var response = await _chatClient.GetResponseAsync(messages, new ChatOptions
            {
                MaxOutputTokens = AgentTokenBudget.DirectChatMaxOutputToken
            });

            // 返回响应文本
            return response.Messages
                .Where(message => message.Role == ChatRole.Assistant)
                .Select(message => message.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? "不客气，有什么其他相关问题也能问我哦。";
        }

        // 在路由type为Unsupported的时候，给出固定无法回答回复
        public AgentAskResponse GenerateUnsupportedResponse(AgentIntentResult intentResult)
        {
            return new AgentAskResponse
            {
                Success = false,
                Recovered = false,
                Message = $"请求不受支持：{intentResult.Reason}",
                Answer =
                """
                抱歉，我目前无法执行这项操作。

                我现在可以帮助你：
                - 查询博客文章
                - 获取并总结文章内容
                - 回答文章中的具体问题
                - 对比不同文章
                - 处理当前会话的上下文
                """
            };
        }

        // 尝试根据本地列表将用户输入解析为对应的行为，得到对应的 Agent 路由事项
        private static AgentIntentResult? TryRouteLocally(string userMessage)
        {
            // 去除首尾空白，便于匹配
            var normalized = userMessage.Trim();

            // 定义本地可识别的重置命令列表
            var resetCommands = new[]
            {
                "清除记忆",
                "重置上下文",
                "忘掉之前的内容",
                "忘记之前的内容",
                "不要参考之前的对话",
                "重新开始"
            };

            // 检查用户消息是否包含重置命令（不区分大小写）
            if (resetCommands.Any(command =>
                normalized.Contains(
                    command,
                    StringComparison.OrdinalIgnoreCase)))
            {
                // 命中重置命令，返回 ResetContext 意图，置信度为 1
                return CreateResult(
                    AgentIntentType.ResetContext,
                    1,
                    "命中明确的本地重置命令");
            }

            // 未匹配任何本地规则，返回 null 交给后续模型处理
            return null;
        }

        // 尝试调用AI将用户输入转化为对应行为
        private async Task<AgentIntentResult?> RouteWithModelAsync(string userMessage)
        {
            // 构建与模型交流消息
            var messages = new List<ChatMessage>
            {
                new(
                    ChatRole.System,
                    """
                    你是博客系统 Agent 的意图分类器。

                    只能从以下四种意图中选择一种：

                    1. ExecuteWorkflow
                       用户要求查询文章、获取正文、总结、对比文章、
                       回答文章内容问题或执行其他博客任务。

                    2. ResetContext
                       用户明确要求忘记、清除或重置此前对话上下文。

                    3. DirectChat
                       用户只是打招呼、感谢、告别或进行不需要工具的简单交流。

                    4. Unsupported
                       用户要求执行系统不支持或不应执行的操作，
                       例如删除文章、修改权限、操作其他用户数据。
                                        
                    - 用户询问“如何删除文章”“如何修改权限”等操作方法，
                      不代表要求 Agent 直接执行，不应识别为 Unsupported。
                    - 用户明确要求 Agent 删除文章、修改权限、操作其他用户数据，
                      才识别为 Unsupported。

                    只输出 JSON，不要输出 Markdown 或解释文字：

                    {
                      "intent": "ExecuteWorkflow",
                      "confidence": 0.95,
                      "reason": "用户要求查询文章"
                    }

                    confidence 必须是 0 到 1 之间的数字。
                    不确定时选择 ExecuteWorkflow。
                    """),

                new(ChatRole.User, userMessage)
            };

            // 获取模型响应
            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.IntentRouterMaxOutputTokens
                }
            );

            // 从响应中获取结果json串文本
            var resultText = response.Messages
                .Where(message => message.Role == ChatRole.Assistant)
                .Select(message => message.Text)
                .FirstOrDefault() ?? string.Empty;

            // 获取干净的json串文本
            resultText = ExtractJson(resultText);

            // 将json串反序列化为ModelIntentResult
            var modelResult = JsonSerializer.Deserialize<ModelIntentResult>(
                resultText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true   // 忽略大小写
                }
            );

            // 如果模型结果不为空且类型合法，转化为对应dto
            if (
                modelResult == null || 
                !System.Enum.TryParse<AgentIntentType>(modelResult.Intent, true, out var intent)
            )
            {
                return CreateResult(AgentIntentType.ExecuteWorkflow, 0, "模型结果无效，当前路由降级为工作流执行。");
            }

            var confidence = Math.Clamp(modelResult.Confidence, 0, 1);

            // 对低置信度不执行特殊分支，防止误清除
            if (confidence < 0.7)
            {
                return CreateResult(AgentIntentType.ExecuteWorkflow, confidence, "意图置信度低，当前路由降级为工作流执行。");
            }

            // 判断为合法路由且置信度较高情况下，执行对应特殊操作，跳过工作流
            return CreateResult(intent, confidence, modelResult.Reason);
        }

        // 创建Agent路由解析结果
        private static AgentIntentResult CreateResult(AgentIntentType intent, double confidence, string reason)
        {
            return new AgentIntentResult(intent, confidence, reason);
        }

        // 从可能包含多余文本的字符串中提取纯 JSON 内容
        // 若找不到有效的JSON串花括号包裹，则返回去除首尾空白后的原始文本
        private static string ExtractJson(string text)
        {
            // 空字符串直接返回空
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // 定位第一个 '{' 和最后一个 '}' 的位置
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            // 如果找到了有效的 JSON 边界，截取中间内容；否则返回去首尾空白的原文本
            return start >= 0 && end > start
                ? text[start..(end + 1)]
                : text.Trim();
        }

        // 接收模型响应的反序列化json文本类
        // 由于intent可能不一定属于枚举类型导致异常，这里暂时不用实际dto
        private sealed class ModelIntentResult
        {
            public string Intent { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
