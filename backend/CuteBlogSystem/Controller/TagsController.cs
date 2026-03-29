using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CuteBlogSystem.Entity;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Service;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
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
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(response);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] Tag tag)
        {
            ApiResponse response = await _tagService.AddTagAsync(tag);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        [HttpPut("{tagId}")]
        public async Task<IActionResult> UpdateTag([FromBody] Tag updatedTag)
        {
            ApiResponse response = await _tagService.UpdateTagAsync(updatedTag);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        [HttpDelete("{tagId}")]
        public async Task<IActionResult> DeleteTag([FromRoute] int tagId)
        {
            ApiResponse response = await _tagService.DeleteTagAsync(tagId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
    }
}
