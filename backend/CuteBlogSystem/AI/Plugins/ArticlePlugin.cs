using CuteBlogSystem.Service;
using CuteBlogSystem.AI.Tools;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;

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
        [Description("当用户询问某个分类下有哪些文章时，获取该分类下的最新发布的文章列表，同时可以指定返回数量")]
        public async Task<string> GetArticlesByCategoryAsync(
            [Description("文章分类名称，例如 技术、生活、随笔")]
            string categoryName,

            [Description("需要返回的文章数量，例如 3 表示返回最新3篇；如未进行指定，则返回数量默认为5篇;最大返回数量为10篇")]
            int count = 5,

            [Description("排序方式。Latest 表示最新发布，MostLiked 表示点赞最多，MostViewed 表示浏览量最多。用户说最新就传 Latest，说点赞最高或最受欢迎就传 MostLiked，说浏览最多或访问量最高就传 MostViewed，用户如果没有明确指明，则默认使用 Latest")]
            string sortBy = "Latest"
        )
        {

            if (count <= 0)
            {
                count = 5; // 默认返回5篇
            }
            else if (count > 10)
            {
                count = 10; // 最大返回10篇
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                _logger.LogWarning("ArticlePlugin.GetArticlesByCategory 被调用时，categoryName 参数为空");
                return "请提供一个有效的分类名称";
            }

            ArticleSortBy articleSortBy;

            if (sortBy == null)
            {
                articleSortBy = ArticleSortBy.Latest;
            }
            else
            {
                articleSortBy = AiHelper.NormalizeSortBy(sortBy);
            }


            _logger.LogInformation(
                "调用 GetArticlesByCategoryAsync，分类：{CategoryName}，数量：{Count}，排序：{SortBy}",
                categoryName,
                count,
                sortBy);

            var response = await _articleService.GetArticlesByCategoryNameAsync(categoryName, count, articleSortBy);

            if (!response.Success)
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

        [KernelFunction]
        [Description("当用户在询问某个标签下有什么文章时，调用此方法")]
        public async Task<string> GetArticlesByTagsAsync(
            [Description("用户提问的标签名称，比如C#，编程，Java，Docker等")]
            string tagName)
        {
            _logger.LogInformation("ArticlePlugin.GetArticlesByTagsAsync 被调用了，参数 tagName: {tagName}", tagName);

            var response = await _articleService.GetArticlesByTagNameAsync(tagName);
            if (response.Data == null)
            {
                _logger.LogError("获取标签下的文章失败: {Message}", response.Message);
                return "未能找到这个标签或标签下没有文章";
            }
            else
            {
                _logger.LogInformation("ArticlePlugin.GetArticlesByTagsAsync 成功获取了标签 {tagName} 下的文章", tagName);
                List<GetArticleListDTO> articles = response.Data as List<GetArticleListDTO>;

                var text = "这个标签的文章如下：\n";
                for (int i = 0; i < articles.Count; i++)
                {
                    var article = articles[i];
                    text += $"{i + 1}. 标题：{article.Title}，发布时间：{article.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
                }
                return text;
            }
        }

        [KernelFunction]
        [Description("根据文章ID获取文章正文内容，用于总结文章、分析文章内容、查看文章详情")]
        public async Task<string> GetArticleContentByIdAsync(
            [Description("文章ID，例如 1、2、3")]
            int articleId)
        {
            _logger.LogInformation("调用 GetArticleContentByIdAsync，文章ID：{ArticleId}", articleId);

            var response = await _articleService.GetArticleContentByIdAsync(articleId);

            if (!response.Success || response.Data == null)
            {
                return $"未找到ID为 {articleId} 的文章内容。";
            }

            return response.Data.ToString();
        }

        [KernelFunction]
        [Description("根据文章ID获取文章的摘要内容")]
        public async Task<string> GetArticleCategoryByIdAsync(
            [Description("文章ID，例如 1、2、3")]
            int articleId)
        {
            _logger.LogInformation("调用 GetArticleCategoryByIdAsync，文章ID：{ArticleId}", articleId);
            var response = await _articleService.GetArticleCategoryByIdAsync(articleId);
            if (!response.Success || response.Data == null)
            {
                return $"未找到ID为 {articleId} 的文章分类。";
            }
            var text = $"文章ID {articleId} 的分类是：{response.Data}";
            return text;
        }

        [KernelFunction]
        [Description("根据文章ID获取文章的标签列表")]
        public async Task<string> GetArticleTagsListByArticleIdAsync(
            [Description("文章ID，例如 1、2、3")]
            int articleId)
        {
            _logger.LogInformation("调用 GetArticleTagsListByArticleIdAsync，文章ID：{ArticleId}", articleId);
            var response = await _articleService.GetArticleTagsListByArticleIdAsync(articleId);
            if (!response.Success || response.Data == null)
            {
                return $"未找到ID为 {articleId} 的文章标签列表。";
            }
            List<string> tags = response.Data as List<string>;
            var text = $"文章ID {articleId} 的标签列表如下：\n";
            for (int i = 0; i < tags.Count; i++)
            {
                text += $"{i + 1}. {tags[i]}\n";
            }
            return text;
        }
    }
}
