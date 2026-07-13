using Microsoft.Extensions.AI;
using SharpToken;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace CuteBlogSystem.Util
{
    public static class AiChatHelper
    {
        private static readonly Lazy<GptEncoding> _encoding = new Lazy<GptEncoding>
        (
            () => GptEncoding.GetEncoding("cl100k_base")  // DeepSeek 使用的编码
        );

        // 以下是 AI 有关静态辅助方法示例，可以根据实际需求进行调整：

        // 一次性输出（非流式）
        public static async Task<string> GetCompleteResponseAsync(
            IChatClient chatClient,
            List<ChatMessage> messages)
        {
            var response = await chatClient.GetResponseAsync(messages);
            string result = response.Messages
                         .Where(m => m.Role == ChatRole.Assistant)
                         .Select(m => m.Text)
                         .FirstOrDefault() ?? string.Empty;
            return result;
        }

        // 流式输出（逐个接收内容片段）
        public static async IAsyncEnumerable<string> GetStreamingResponseAsync(
            IChatClient chatClient,
            List<ChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    yield return update.Text;
                }
            }
        }

        // 构造消息
        public static List<ChatMessage> BuildChatMessages(string system, string userMessage)
        {
            return new List<ChatMessage>
            {
                new(ChatRole.System, system),
                new(ChatRole.User, userMessage)
            };
        }

        // 统计 Token 数量
        public static int CountTokens(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            var encoding = _encoding.Value;
            var tokens = encoding.Encode(input);
            return tokens.Count;
        }

        // 从参数字典中安全获取字符串值，支持 JsonElement 类型，不存在或解析失败时返回默认值
        public static string GetString(
            Dictionary<string, object>? parameters,
            string key,
            string defaultValue = "")
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out var value) ||
                value == null)
            {
                return defaultValue;
            }

            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind == JsonValueKind.String
                    ? jsonElement.GetString() ?? defaultValue
                    : jsonElement.ToString();
            }

            return value.ToString() ?? defaultValue;
        }

        // 从参数字典中安全获取整数值，支持 JsonElement（数字或数字字符串），不存在或解析失败时返回默认值
        public static int GetInt(
            Dictionary<string, object>? parameters,
            string key,
            int defaultValue = 0)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out var value) ||
                value == null)
            {
                return defaultValue;
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number &&
                    jsonElement.TryGetInt32(out var number))
                {
                    return number;
                }

                if (jsonElement.ValueKind == JsonValueKind.String &&
                    int.TryParse(jsonElement.GetString(), out var stringNumber))
                {
                    return stringNumber;
                }

                return defaultValue;
            }

            return int.TryParse(value.ToString(), out var result)
                ? result
                : defaultValue;
        }

        // 检查参数字典中是否存在指定键
        public static bool HasKey(
            Dictionary<string, object>? parameters,
            string key)
        {
            return parameters?.ContainsKey(key) == true;
        }
    }

    public static class AgentVersionConstants
    {
        // 以下是一些示例常量，表示不同版本的 Agent 相关功能或配置：

        // 计划器提示版本
        public const string PlannerPromptVersion = "planner-prompt-v1";

        // 任务提示版本
        public const string ActionRegistryVersion = "action-registry-v1";

        // 评估版本
        public const string EvaluationVersion = "evaluation-v1";

        // 最终答案提示版本
        public const string FinalAnswerPromptVersion = "final-answer-v1";
    }
}