using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.Blog
{
    public class GetArticleListDTO
    {
        // 文章创建时间
        public DateTime CreatedAt { get; set; }

        // 文章标题
        public string Title { get; set; } = string.Empty;

        // 文章摘要
        public string Summary { get; set; } = string.Empty;

        // 文章封面图URL
        public string CoverUrl { get; set; } = string.Empty;

        // 浏览次数
        public int ViewCount { get; set; }

        // 点赞次数
        public int LikeCount { get; set; }

        // 所属分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 关联的标签名称列表
        public List<string> TagNames { get; set; } = new List<string>();

        // 文章唯一标识
        public int Id { get; set; }

        // 无参构造
        public GetArticleListDTO() { }

        // 根据Article实体构造DTO
        public GetArticleListDTO(Article article)
        {
            CreatedAt = article.CreatedAt;
            Title = article.Title;
            Summary = article.Summary;
            CoverUrl = article.CoverUrl;
            ViewCount = article.ViewCount;
            LikeCount = article.LikeCount;
            CategoryName = article.Category?.Name ?? "未分类";
            TagNames = article.ArticleTags.Select(at => at.Tag.Name).ToList() ?? new List<string>();
            Id = article.Id;
        }
    }
}
