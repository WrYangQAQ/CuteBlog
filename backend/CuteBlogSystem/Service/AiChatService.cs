using CuteBlogSystem.Util;
using Microsoft.Extensions.AI;

namespace CuteBlogSystem.Service
{
    public class AiChatService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<AiChatService> _logger;

        public AiChatService(IChatClient chatClient, ILogger<AiChatService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        // 处理用户消息并获取 AI 的响应，一次性返回 AI 完整的回答
        public async Task<string> AskAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                throw new ArgumentException("用户消息不能为空", nameof(userMessage));
            }

            var messages = AiChatHelper.BuildChatMessages("你是一个专门讲解 ASP.NET Core 和 Agent 开发的老师，回答清晰、准确、详细。", 
                                                          userMessage);

            _logger.LogInformation("开始调用 AI，用户消息：{Message}", userMessage);

            var response = await _chatClient.GetResponseAsync(messages);  // response 为 ChatResponse 类型，不是自定义的 ApiResponse 类型

            string result = await AiChatHelper.GetCompleteResponseAsync(_chatClient, messages);  // 获取 AI 的完整响应文本

            _logger.LogInformation("AI 返回成功，响应长度：{Length}", result.Length);

            return result;
        }

        // 使用 IAsyncEnumerable<string> 来实现流式响应，逐步返回 AI 的回答
        public async IAsyncEnumerable<string> AskStreamingAsync(string userMessage,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var messages = AiChatHelper.BuildChatMessages("你是一个专门讲解 ASP.NET Core 和 Agent 开发的老师，回答清晰、准确、简洁。", 
                                                          userMessage);

            await foreach (var update in AiChatHelper.GetStreamingResponseAsync(_chatClient, messages, cancellationToken))
            {
                yield return update;
            }
        }
    }
}