using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiAgentController : BaseController
    {
        private readonly AiAgentService _agentService;
        private readonly ILogger<AiAgentController> _logger;

        public AiAgentController(AiAgentService agentService, ILogger<AiAgentController> logger)
        {
            _agentService = agentService;
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
                var result = await _agentService.AskAsync(request.Message);
                return Ok(new { answer = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent 调用失败");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
