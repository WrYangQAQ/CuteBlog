using CuteBlogSystem.DTO;
using CuteBlogSystem.Service;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace CuteBlogSystem.AI.Plugins
{
    public class ArticlePlugin
    {
        private readonly ArticleService _articleService;
        private readonly ILogger<ArticlePlugin> _logger;

        public ArticlePlugin(ArticleService articleService, ILogger<ArticlePlugin> logger)
        {
            _articleService = articleService;
            _logger = logger;
        }

        [KernelFunction]
        [Description("当用户询问最近文章、最新文章有哪些文章、最近发布了什么时，获取最近发布的文章列表")]
        public async Task<string> GetLatestArticlesAsync()
        {
            _logger.LogInformation("ArticlePlugin.GetLatestArticles 被调用了");

            var response = await _articleService.GetLatestArticlesAsync();
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("获取最新文章失败: {Message}", response.Message);
                return $"获取最新文章失败: {response.Message}";
            }

            var articles = response.Data as List<GetArticleListDTO>;
            if (articles == null || articles.Count == 0)
            {
                _logger.LogInformation("没有找到最新文章");
                return "没有找到最新文章";
            }

            var text = "最近发布的文章如下：\n";
            for (int i = 0; i < articles.Count; i++)
            {
                var article = articles[i];
                text += $"{i + 1}. 标题：{article.Title}，发布时间：{article.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
            }

            _logger.LogInformation("ArticlePlugin.GetLatestArticles 返回了 {Count} 篇文章", articles.Count);

            return text;
        }

        [KernelFunction]
        [Description("当用户询问某个分类下有哪些文章时，获取该分类下的所有文章列表")]
        public async Task<string> GetArticlesByCategoryAsync(
            [Description("文章分类名称，例如 技术、生活、随笔")]
            string categoryName
        )
        {
            _logger.LogInformation("ArticlePlugin.GetArticlesByCategory 被调用了，参数 categoryName: {CategoryName}", categoryName);
            
            var response = await _articleService.GetArticlesByCategoryNameAsync(categoryName);
            
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("获取分类下的文章失败: {Message}", response.Message);
                return $"获取分类下的文章失败: {response.Message}";
            }
            
            var articles = response.Data as List<GetArticleListDTO>;
            
            if (articles == null || articles.Count == 0)
            {
                _logger.LogInformation("没有找到分类 {CategoryName} 下的文章", categoryName);
                return $"没有找到分类 {categoryName} 下的文章";
            }
            
            var text = $"分类 {categoryName} 下的文章如下：\n";
            
            for (int i = 0; i < articles.Count; i++)
            {
                var article = articles[i];
                text += $"{i + 1}. 标题：{article.Title}，发布时间：{article.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
            }
            
            _logger.LogInformation("ArticlePlugin.GetArticlesByCategory 返回了 {Count} 篇文章", articles.Count);
            
            return text;
        }
    }
}
