using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace CuteBlogSystem.Util
{
    public static class AiChatHelper
    {
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

    }
}