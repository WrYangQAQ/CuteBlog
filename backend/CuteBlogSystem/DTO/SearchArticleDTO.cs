namespace CuteBlogSystem.DTO
{
    public class SearchArticleDTO
    {
        public string? Keyword { get; set; }
        public List<string>? ArticleTag { get; set; }
        public string? Category { get; set; }

        public SearchArticleDTO() { }
    }
}
