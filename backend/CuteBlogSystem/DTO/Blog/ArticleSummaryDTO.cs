using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.Blog
{
    public class ArticleSummaryDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public ArticleSummaryDTO(Article article)
        {
            Id = article.Id;
            Title = article.Title;
            CoverUrl = article.CoverUrl;
            ViewCount = article.ViewCount;
            LikeCount = article.LikeCount;
            CreatedAt = article.CreatedAt;
        }
    }
}
