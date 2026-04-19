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
    public class ArticlesController : BaseController
    {
        private readonly ArticleService _articleService;
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(ArticleService articleService, ILogger<ArticlesController> logger)
        {
            _articleService = articleService;
            _logger = logger;

        }


        // 获取文章列表
        [Authorize]
        [HttpGet()]
        public async Task<IActionResult> GetArticlesList()
        {
            ApiResponse response = await _articleService.GetAllArticlesAsync();
            return ReturnResponse(response);
        }

        // 模糊搜索
        [Authorize]
        [HttpGet("search")]
        public async Task<IActionResult> SearchArticles([FromQuery] SearchArticleDTO searchArticleDTO)
        {
            ApiResponse response = await _articleService.SearchArticlesAsync(searchArticleDTO);
            return ReturnResponse(response);
        }


        // 获取文章内容
        [Authorize]
        [HttpGet("{articleId}")]
        public async Task<IActionResult> GetArticlesContentById([FromRoute] int articleId)
        {
            ApiResponse response = await _articleService.GetArticleContentByIdAsync(articleId);
            return ReturnResponse(response);
        }

        // 阅读文章，增加阅读量
        [Authorize]
        [HttpPost("read/{articleId}")]
        public async Task<IActionResult> ReadArticle([FromRoute] int articleId, [FromBody] int stayDuration)
        {
            ApiResponse response = await _articleService.IncrementArticleViewCountAsync(articleId, stayDuration);
            return ReturnResponse(response);
        }

        // 上传文章封面图片
        [Authorize]
        [HttpPost("cover")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadArticleCover([FromForm] UploadImageRequest request)
        {
            ApiResponse response = await _articleService.UploadArticleCoverAsync(request.Image);
            return ReturnResponse(response);
        }

        // 发布文章
        [Authorize]
        [HttpPost("publish")]
        public async Task<IActionResult> PublishArticle([FromBody] PublishArticleDTO article)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);

            if (success)
            {
                ApiResponse response = await _articleService.PublishArticleAsync(article, userId);
                return ReturnResponse(response);
            }
            else
            {
                return ReturnResponse(new ApiResponse(false, "用户未认证", code:ResponseCode.Unauthorized));
            }
        }

        // 切换点赞状态
        [Authorize]
        [HttpPost("{articleId}/like")]
        public async Task<IActionResult> LikeArticle([FromRoute] int articleId)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            if (success)
            {
                ApiResponse response = await _articleService.ToggleArticleLikeAsync(articleId, userId);
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("用户未认证，无法点赞文章");
                return ReturnResponse(new ApiResponse(false, "用户未认证", code: ResponseCode.Unauthorized));
            }
        }

        // 删除文章（同时删除关联的标签、评论和点赞记录）
        // 只有文章作者或管理员可以删除文章
        [Authorize]
        [HttpDelete("{articleId}")]
        public async Task<IActionResult> DeleteArticle([FromRoute] int articleId)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            if (!success)
            {
                _logger.LogWarning("用户未认证，无法删除文章");
                return ReturnResponse(new ApiResponse(false, "用户未认证", code: ResponseCode.Unauthorized));
            }
            ApiResponse response = await _articleService.DeleteArticleAsync(articleId, userId);
            return ReturnResponse(response);

        }

        // 编辑文章
        // 只有文章作者或管理员可以编辑文章
        [Authorize]
        [HttpPut("{articleId}")]
        public async Task<IActionResult> EditArticle([FromRoute] int articleId, [FromBody] UpdateArticleDTO updateArticleDTO)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId);
            if (!success)
            {
                _logger.LogWarning("用户未认证，无法编辑文章");
                return ReturnResponse(new ApiResponse(false, "用户未认证", code: ResponseCode.Unauthorized));
            }
            ApiResponse response = await _articleService.UpdateArticleContentAsync(articleId, updateArticleDTO, userId);
            return ReturnResponse(response);
        }

        // 置顶文章（或者取消置顶）
        // 只有管理员可以置顶文章
        [Authorize(Roles = "Admin")]
        [HttpPost("{articleId}/top")]
        public async Task<IActionResult> PutArticleOnTopAsync([FromRoute] int articleId)
        {
            ApiResponse response = await _articleService.ToggleArticleTopAsync(articleId);
            return ReturnResponse(response);

        }

        // 获取置顶文章
        [Authorize]
        [HttpGet("topped")]
        public async Task<IActionResult> GetToppedArticlesAsync()
        {
            ApiResponse response = await _articleService.GetTopArticlesAsync();
            return ReturnResponse(response);
        }

        // 推荐文章（或者取消推荐）
        // 只有管理员可以推荐文章
        [Authorize(Roles = "Admin")]
        [HttpPost("{articleId}/recommend")]
        public async Task<IActionResult> RecommendArticleAsync([FromRoute] int articleId)
        {
            ApiResponse response = await _articleService.ToggleArticleRecommendAsync(articleId);
            return ReturnResponse(response);
        }

        // 获取推荐文章
        [Authorize]
        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommendedArticlesAsync()
        {
            ApiResponse response = await _articleService.GetRecommendArticlesAsync();
            return ReturnResponse(response);
        }
    }
}
