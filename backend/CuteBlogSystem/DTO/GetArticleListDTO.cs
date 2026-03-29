using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class GetArticleListDTO
    {
        public DateTime CreatedAt { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string CoverUrl { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public string CategoryName { get; set; }
        public List<string> TagNames { get; set; } = new List<string>();
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
            TagNames = article.ArticleTags.Select(at => at.Tag.Name).ToList();
            Id = article.Id;
        }
    }
}
