using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CuteBlogSystem.Service
{
    public class AiAgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<AiAgentService> _logger;

        public AiAgentService(Kernel kernel, ILogger<AiAgentService> logger)
        {
            _kernel = kernel;
            _logger = logger;
        }

        public async Task<string> AskAsync(string userMessage)
        {

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                throw new ArgumentException("消息不能为空", nameof(userMessage));
            }

            _logger.LogInformation("Agent 开始处理请求：{Message}", userMessage);

            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var result = await _kernel.InvokePromptAsync(
                """
                你是博客系统助手。
                当用户询问最近文章、最新文章、有哪些文章时，请使用可用工具获取真实数据后再回答。
                不要凭空回答。
                """
                + $"\n用户问题：{userMessage}",
                new(settings)
            );

            var text = result.ToString();

            _logger.LogInformation("Agent 返回结果长度：{Length}", text.Length);

            return text;
        }
    }
}