using Azure.Core;
using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiChatController : BaseController
    {
        private readonly AiChatService _aiChatService;
        private readonly ILogger<AiChatController> _logger;

        public AiChatController(AiChatService aiChatService, ILogger<AiChatController> logger)
        {
            _aiChatService = aiChatService;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AiChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("消息不能为空");
            }

            try
            {
                var answer = await _aiChatService.AskAsync(request.Message);
                return Ok(new { answer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI 对话失败");
                return StatusCode(500, "AI 对话失败");
            }
        }

        // SSE 方式实现流式响应
        [HttpPost("ask-stream")]
        public async Task AskStream([FromBody] AiChatRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                Response.StatusCode = 400;     // Response是ControllerBase的属性，直接使用
                await Response.WriteAsync("消息不能为空", cancellationToken);
                return;
            }

            Response.StatusCode = 200;
            Response.ContentType = "text/event-stream; charset=utf-8";

            // 防止缓存
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connnection"] = "keep-alive";

            await foreach (var chunk in _aiChatService.AskStreamingAsync(request.Message, cancellationToken))
            {
                // SSE 格式：每个数据块以 "data: " 开头，后面跟数据内容，最后以两个换行符结束
                var data = $"data: {chunk}\n\n";

                await Response.WriteAsync(data, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            // 发送流结束标志
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
