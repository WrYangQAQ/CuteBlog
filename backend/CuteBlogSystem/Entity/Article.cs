namespace CuteBlogSystem.Entity
{
    public class Article
    {
        public Article() { }

        public Article(int id, int userId, string title, string summary, string content, string coverUrl, int viewCount, int likeCount, bool isTop, bool isRecommend, int categoryId, DateTime createdAt, DateTime updatedAt)
        {
            Id = id;
            UserId = userId;
            Title = title;
            Summary = summary;
            Content = content;
            CoverUrl = coverUrl;
            ViewCount = viewCount;
            LikeCount = likeCount;
            IsTop = isTop;
            IsRecommend = isRecommend;
            CategoryId = categoryId;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string CoverUrl { get; set; }
        public int ViewCount { get; set; } = 0;
        public int LikeCount { get; set; } = 0;
        public bool IsTop { get; set; }
        public bool IsRecommend { get; set; }
        public int CategoryId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        // 导航属性
        public virtual Category Category { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<ArticleLike> ArticleLikes { get; set; } = new List<ArticleLike>();
    }
}
