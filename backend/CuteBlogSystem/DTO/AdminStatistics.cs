using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class AdminStatistics
    {
        public int TotalArticles { get; set; }
        public int TotalComments { get; set; }
        public int TotalCategories { get; set; }
        public int TotalTags { get; set; }

        // 近七天每天发布的文章数量
        public List<int> ArticlesLast7Days { get; set; } = new List<int>();

        // 阅读量最高的五篇文章
        public List<ArticleSummaryDTO> Top5ArticlesByViews { get; set; } = new();

        public AdminStatistics() { }
    }
}
