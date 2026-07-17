using Microsoft.SemanticKernel;
using System.ComponentModel;
using CuteBlogSystem.Service;
using CuteBlogSystem.DTO.Blog;

namespace CuteBlogSystem.AI.Plugins
{
    public class CategoryPlugin
    {
        private readonly CategoryService _categoryService;
        private readonly ILogger<CategoryPlugin> _logger;

        public CategoryPlugin(CategoryService categoryService, ILogger<CategoryPlugin> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        [KernelFunction]
        [Description("当用户询问有哪些分类、博客分类列表、文章分类时，获取所有分类")]
        public async Task<string> GetAllCategoriesAsync()
        {
            _logger.LogInformation("CategoryPlugin 被调用");

            var result = await _categoryService.GetAllCategoriesAsync();

            if (!result.Success || result.Data == null)
            {
                return "获取分类失败";
            }
            else
            {
                List<string>? categories = (result.Data as List<GetCategoryDTO>)?.Select(c => c.Name).ToList();

                if (categories == null || categories.Count == 0)
                {
                    return "当前没有分类";
                }

                var text = "当前分类如下：\n";

                for (int i = 0; i < categories.Count; i++)
                {
                    text += $"{i + 1}. {categories[i]}\n";
                }

                return text;
            }
            
        }
    }
}