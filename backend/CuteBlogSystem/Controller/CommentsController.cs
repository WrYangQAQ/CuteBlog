using CuteBlogSystem.DTO;
using CuteBlogSystem.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly CommentService _commentService;
        private readonly ILogger<CommentsController> _logger;
        public CommentsController(CommentService commentService)
        {
            _commentService = commentService;
        }

        [Authorize]
        [HttpPost()]
        public async Task<IActionResult> PublishComment([FromBody] PublishCommentDTO commentDTO, [FromQuery] int articleId)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            if (success)
            {
                ApiResponse response = await _commentService.PublishCommentAsync(commentDTO, userId, articleId);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            else
            {
                _logger.LogWarning("请求头携带JWT已失效，请重新登陆！");
                return Unauthorized(new ApiResponse(false, "请求头携带JWT已失效，请重新登陆！"));
            }
        }

        [Authorize]
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment([FromRoute] int commentId)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            if (success)
            {
                ApiResponse response = await _commentService.DeleteCommentAsync(commentId, userId);
                if (response.Success)
                {
                    return Ok(response);
                }
                else if (response.Code == ResponseCode.Unauthorized)
                {
                    return Unauthorized(response);
                }
                else if (response.Code == ResponseCode.NotFound)
                {
                    return NotFound(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            else
            {
                _logger.LogWarning("请求头携带JWT已失效，请重新登陆！");
                return Unauthorized(new ApiResponse(false, "请求头携带JWT已失效，请重新登陆！"));
            }
        }

        [HttpGet()]
        public async Task<IActionResult> GetCommentsLists([FromQuery] int articleId)
        {
            ApiResponse response = await _commentService.GetCommentsListAsync(articleId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(response);
            }
        }
    }
}
