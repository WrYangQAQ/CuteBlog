namespace CuteBlogSystem.DTO
{
    public class UpdateArticleDTO
    {
        public int ArticleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public List<int> TagIds { get; set; } = new List<int>();
        public UpdateArticleDTO() { }
    }

    public class UpdateArticleInformation
    {
        public int ArticleId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int OldLength { get; set; }

        public int NewLength { get; set; }

        public DateTime UpdatedAt { get; set; }

        public UpdateArticleInformation() { }
    }
}
