using CuteBlogSystem.DTO;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CuteBlogSystem.Config;

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

        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            ApiResponse response = await _tagService.GetAllTagsAsync();
            return ReturnResponse(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] Tag tag)
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
    }
}
