using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class DisplayArticleDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string AuthorName { get; set; }
        public string CategoryName { get; set; }
        public List<string> TagNames { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }

        // 根据Article实体构造DTO
        public DisplayArticleDTO(Article article)
        {
            Title = article.Title;
            Content = article.Content;
            AuthorName = article.User?.UserName ?? "未知作者";
            CategoryName = article.Category?.Name ?? "未分类";
            TagNames = article.ArticleTags.Select(at => at.Tag.Name).ToList();
            CreatedAt = article.CreatedAt;
        }
    }
}
