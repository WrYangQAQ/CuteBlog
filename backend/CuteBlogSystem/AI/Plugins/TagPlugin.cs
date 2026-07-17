using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Service;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace CuteBlogSystem.AI.Plugins
{
    public class TagPlugin
    {
        private readonly TagService _tagService;
        private readonly CategoryService _categoryService;
        private readonly ILogger<TagPlugin> _logger;

        public TagPlugin(TagService tagService, ILogger<TagPlugin> logger, CategoryService categoryService)
        {
            _tagService = tagService;
            _categoryService = categoryService;
            _logger = logger;
        }

        [KernelFunction]
        [Description("当用户询问某个分类下有哪些标签、标签列表、标签有哪些时，获取该分类下的所有标签")]
        public async Task<string> GetTagsAsync()
        {
            _logger.LogInformation("TagPlugin.GetTagsAsync 被调用了");

            var response = await _tagService.GetAllTagsAsync();

            if (!response.Success || response.Data == null)
            {
                _logger.LogError("获取标签失败: {Message}", response.Message);
                return $"获取标签失败: {response.Message}";
            }

            var tags = response.Data as List<GetTagDTO>;

            if (tags == null || tags.Count == 0)
            {
                _logger.LogInformation("没有找到标签");
                return "没有找到标签。";
            }

            // 构造返回文本
            var text = "以下是所有标签：\n";
            foreach (var tag in tags)
            {
                text += $"标签名：{tag.Name}\n";
                string categoryName = await _categoryService.GetCategoryByIdAsync(tag.CategoryId);
                text += $"所属分类：{categoryName}\n";
            }
            return text;
        }

        
    }
}
