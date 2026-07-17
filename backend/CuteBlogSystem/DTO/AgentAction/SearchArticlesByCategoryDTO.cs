using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    // 搜索文章动作的输入参数
    public class SearchArticlesByCategoryInput
    {
        // 文章分类名称（必填）
        public string CategoryName { get; set; } = string.Empty;

        // 排序方式：MostLiked（按点赞）、MostViewed（按浏览）、Latest（按最新）等
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.MostLiked;

        // 返回的最大文章数量（默认5，建议1~10）
        public int Top { get; set; } = 5;
    }

    // 搜索文章动作的输出结果
    public class SearchArticlesByCategoryOutput : IUserReadableOutput, IAgentArticleReferenceOutput
    {
        // 匹配的文章列表（可能为空）
        public List<ArticleSearchResultItem> Articles { get; set; } = new();

        // 实际查询的分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 实际使用的排序方式（默认时间排序）
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.Latest;

        // 该分类下的总文章数（用于分页或提示）
        public int TotalCount { get; set; }

        public string ToUserReadableText()
        {
            if (Articles.Count == 0)
            {
                return $"分类「{CategoryName}」下没有找到文章。";
            }

            var articleLines = Articles.Select((article, index) =>
                $"{index + 1}. {article.Title}，分类：{article.CategoryName}，点赞数：{article.LikeCount}，浏览量：{article.ViewCount}");

            return $"""
            分类：{CategoryName}
            排序方式：{SortBy}
            文章数量：{TotalCount}

            {string.Join("\n", articleLines)}
            """;
        }

        public int? GetPrimaryArticleId()
        {
            return Articles.FirstOrDefault()?.Id;
        }
    }

    // 文章搜索结果项（概要信息）
    public class ArticleSearchResultItem
    {
        // 文章唯一标识
        public int Id { get; set; }

        // 文章标题
        public string Title { get; set; } = string.Empty;

        // 文章摘要（通常为前200字）
        public string Summary { get; set; } = string.Empty;

        // 文章封面图URL（可选）
        public string? CoverUrl { get; set; }

        // 浏览次数
        public int ViewCount { get; set; }

        // 点赞次数
        public int LikeCount { get; set; }

        // 所属分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 关联的标签名称列表
        public List<string> TagNames { get; set; } = new();

        // 构造函数：从 GetArticleListDTO 映射
        public ArticleSearchResultItem(GetArticleListDTO dto)
        {
            Id = dto.Id;
            Title = dto.Title;
            Summary = dto.Summary;
            CoverUrl = string.IsNullOrEmpty(dto.CoverUrl) ? null : dto.CoverUrl; // 转为可空
            ViewCount = dto.ViewCount;
            LikeCount = dto.LikeCount;
            CategoryName = dto.CategoryName;
            TagNames = new List<string>(dto.TagNames); // 拷贝新列表，避免引用共享
        }
    }
}
