using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.DTO.Blog;
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
    public class MessagesController : BaseController
    {
        private readonly CommentService _commentService;
        private readonly ILogger<MessagesController> _logger;
        private readonly IConfiguration _configuration;
        public MessagesController(CommentService commentService, 
            ILogger<MessagesController> logger,
            IConfiguration configuration)
        {
            _commentService = commentService;
            _logger = logger;
            _configuration = configuration;
        }


        [Authorize]
        [HttpPost()]
        public async Task<IActionResult> PublishComment([FromBody] PublishCommentDTO commentDTO)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            if (success)
            {
                bool articleIdSuccess = int.TryParse(_configuration.GetSection("MessageBoardArticleId").Value, out int articleId);
                if (!articleIdSuccess)
                {
                    _logger.LogError("配置项 MessageBoardArticleId 未正确设置，请检查 appsettings.json 文件！");
                    return ReturnResponse(new ApiResponse(false, "服务器配置错误，请稍后再试！", code: ResponseCode.InternalError));
                }
                ApiResponse response = await _commentService.PublishCommentAsync(commentDTO, userId, articleId);
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("请求头携带JWT已失效，请重新登陆！");
                return ReturnResponse(new ApiResponse(false, "请求头携带JWT已失效，请重新登陆！", code: ResponseCode.Unauthorized));
            }
        }

        [Authorize]
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment([FromRoute] int commentId)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            if (success)
            {
                ApiResponse response = await _commentService.DeleteMessageAsync(commentId, userId);
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("请求头携带JWT已失效，请重新登陆！");
                return ReturnResponse(new ApiResponse(false, "请求头携带JWT已失效，请重新登陆！", code: ResponseCode.Unauthorized));
            }
        }

        [HttpGet()]
        public async Task<IActionResult> GetCommentsLists()
        {
            bool success = int.TryParse(_configuration.GetSection("MessageBoardArticleId").Value, out int articleId);
            if (!success)
            {
                _logger.LogError("配置项 MessageBoardArticleId 未正确设置，请检查 appsettings.json 文件！");
                return ReturnResponse(new ApiResponse(false, "服务器配置错误，请稍后再试！", code: ResponseCode.InternalError));
            }
            ApiResponse response = await _commentService.GetCommentsListAsync(articleId);
            return ReturnResponse(response);
        }
    }
}
