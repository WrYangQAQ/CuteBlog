using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiAgentController : BaseController
    {
        private readonly AiAgentService _agentService;
        private readonly AiPlannerService _plannerService;
        private readonly AgentWorkflowService _workflowService;
        private readonly AgentWorkflowLogService _workflowLogService;
        private readonly AgentMessageService _messageService;
        private readonly AgentPendingConfirmationService _pendingConfirmationService;
        private readonly ILogger<AiAgentController> _logger;

        public AiAgentController(AiAgentService agentService,
                                 ILogger<AiAgentController> logger,
                                 AiPlannerService plannerService,
                                 AgentWorkflowService workflowService,
                                 AgentWorkflowLogService workflowLogService,
                                 AgentMessageService messageService,
                                 AgentPendingConfirmationService pendingConfirmationService)
        {
            _agentService = agentService;
            _logger = logger;
            _plannerService = plannerService;
            _workflowService = workflowService;
            _workflowLogService = workflowLogService;
            _messageService = messageService;
            _pendingConfirmationService = pendingConfirmationService;
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

        [HttpPost("plan")]
        public async Task<IActionResult> AskPlanner([FromBody] AiChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("消息不能为空");
            }
            try
            {
                var plan = await _plannerService.CreatePlanAsync(request.Message);
                return Ok(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent 计划生成失败");
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("planner-ask")]
        public async Task<IActionResult> PlannerAsk(
            [FromBody] AgentUserMessage agentUserMessage,
            [FromQuery] bool debug = false)
        {
            if (agentUserMessage == null || string.IsNullOrWhiteSpace(agentUserMessage.Content))
            {
                return BadRequest("消息不能为空");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("userId")
                  ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            agentUserMessage.UserId = userId;

            var response = await _workflowService.AskAsync(agentUserMessage, debug);

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var response = await _workflowLogService.GetAllLogsAsync();
            return ReturnResponse(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("logs/range")]
        public async Task<IActionResult> GetLogsByTimeRange(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {

            var response = await _workflowLogService.GetLogsByTimeRangeAsync(startTime, endTime);
            return ReturnResponse(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("logs/{id}")]
        public async Task<IActionResult> GetLogById([FromRoute] int id)
        {
            var response = await _workflowLogService.GetLogByIdAsync(id);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            // 从 JWT 的 claims 中获取用户 ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("userId")
                              ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            if (userId <= 0)
            {
                return BadRequest("userId 不能为空");
            }

            var response = await _messageService.GetConversationsByUserIdAsync(userId);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpGet("conversations/{sessionId}/messages")]
        public async Task<IActionResult> GetMessagesBySessionId([FromRoute] string sessionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("userId")
                  ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("SessionId 不能为空");
            }

            var response = await _messageService.GetMessagesBySessionIdAsync(sessionId, userId);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpDelete("conversations/{sessionId}")]
        public async Task<IActionResult> DeleteConversation([FromRoute] string sessionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("userId")
                  ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("SessionId 不能为空");
            }

            var response = await _messageService.DeleteConversationAsync(sessionId, userId);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpPatch("conversations/{sessionId}/archive")]
        public async Task<IActionResult> ArchiveConversation([FromRoute] string sessionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("userId")
                  ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            var response = await _messageService.ArchiveConversationAsync(sessionId, userId);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpPatch("conversations/{sessionId}/restore")]
        public async Task<IActionResult> RestoreConversation([FromRoute] string sessionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("userId")
                  ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            var response = await _messageService.RestoreConversationAsync(sessionId, userId);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpGet("conversations/archived")]
        public async Task<IActionResult> GetArchivedConversations()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("userId")
                  ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            var response = await _messageService.GetArchivedConversationsAsync(userId);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpPost("cancel-confirmation")]
        public async Task<IActionResult> CancelConfirmation([FromBody] AgentConfirmationRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("userId")
                  ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            var success = await _pendingConfirmationService.CancelAsync(
                request.ConfirmationId,
                userId.ToString(),
                request.SessionId);

            if (!success)
            {
                return BadRequest(new ApiResponse(false, "取消确认请求失败"));
            }

            return Ok(new ApiResponse(true, "已取消该操作"));
        }

        [Authorize]
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] AgentConfirmationRequest request, [FromQuery] bool debug = false)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("userId")
                  ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                return Unauthorized("无法从令牌中解析用户标识");
            }

            var response = await _workflowService.ConfirmAsync(
                request.ConfirmationId,
                request.SessionId,
                userId,
                debug);

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("workflow-logs/recent")]
        public async Task<IActionResult> GetRecentWorkflowLogs([FromQuery] int count = 20)
        {
            var response = await _workflowLogService.GetRecentLogAsync(count);
            return ReturnResponse(response);
        }
    }
}