using CuteBlogSystem.DTO;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CuteBlogSystem.Config;
using CuteBlogSystem.DTO.Blog;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : BaseController
    {
        private readonly TagService _tagService;
        public TagsController(TagService tagService)
        {
            _tagService = tagService;
        }

        // 获取所有标签
        [HttpGet("all")]
        public async Task<IActionResult> GetAllTags()
        {
            ApiResponse response = await _tagService.GetAllTagsAsync();
            return ReturnResponse(response);
        }

        // 根据分类获取标签
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetTagsByCategoryId([FromRoute] int categoryId)
        {
            ApiResponse response = await _tagService.GetTagsByCategoryIdAsync(categoryId);
            return ReturnResponse(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] GetTagDTO tag)
        {
            ApiResponse response = await _tagService.AddTagAsync(tag);
            return ReturnResponse(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{tagId}")]
        public async Task<IActionResult> UpdateTag([FromBody] Tag updatedTag, [FromRoute] int tagId)
        {
            ApiResponse response = await _tagService.UpdateTagAsync(updatedTag, tagId);
            return ReturnResponse(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{tagId}")]
        public async Task<IActionResult> DeleteTag([FromRoute] int tagId)
        {
            ApiResponse response = await _tagService.DeleteTagAsync(tagId);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpGet("articleCounts/{tagId}")]
        public async Task<IActionResult> GetArticlesCountsByTagIdAsync([FromRoute] int tagId)
        {
            ApiResponse response = await _tagService.GetArticleCountByTagIdAsync(tagId);
            return ReturnResponse(response);
        }

        [Authorize]
        [HttpPost("articleCounts/batch")]
        public async Task<IActionResult> GetArticleCountsByTagIdsAsync([FromBody] List<int> tagIds)
        {
            ApiResponse response = await _tagService.GetArticleCountsByTagIdsAsync(tagIds);
            return ReturnResponse(response);
        }
    }
}
