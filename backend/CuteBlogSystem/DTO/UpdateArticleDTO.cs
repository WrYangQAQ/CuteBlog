namespace CuteBlogSystem.DTO
{
    public class UpdateArticleDTO
    {
        public int ArticleId { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public int CategoryId { get; set; }
        public List<int> TagIds { get; set; } = new List<int>();
        public UpdateArticleDTO() { }

        public UpdateArticleDTO(string title, string summary, string content, int categoryId, List<int> tagIds)
        {
            Title = title;
            Summary = summary;
            Content = content;
            CategoryId = categoryId;
            TagIds = tagIds;
        }
    }
}
