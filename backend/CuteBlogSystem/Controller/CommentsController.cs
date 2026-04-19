using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : BaseController
    {
        private readonly CommentService _commentService;
        private readonly ILogger<CommentsController> _logger;
        public CommentsController(CommentService commentService, ILogger<CommentsController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost()]
        public async Task<IActionResult> PublishComment([FromBody] PublishCommentDTO commentDTO, [FromQuery] int articleId)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            if (success)
            {
                ApiResponse response = await _commentService.PublishCommentAsync(commentDTO, userId, articleId);
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("请求头携带JWT已失效，请重新登陆！");
                return ReturnResponse(new ApiResponse(false, "请求头携带JWT已失效，请重新登陆！", code:ResponseCode.Unauthorized));
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
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("请求头携带JWT已失效，请重新登陆！");
                return ReturnResponse(new ApiResponse(false, "请求头携带JWT已失效，请重新登陆！", code: ResponseCode.Unauthorized));
            }
        }

        [HttpGet()]
        public async Task<IActionResult> GetCommentsLists([FromQuery] int articleId)
        {
            ApiResponse response = await _commentService.GetCommentsListAsync(articleId);
            return ReturnResponse(response);
        }
    }
}
