using CuteBlogSystem.Repository;
using CuteBlogSystem.DTO;

namespace CuteBlogSystem.Service
{
    public class AdminStatisticsService
    {
        private readonly ArticleRepository _articleRepository;
        private readonly CommentRepository _commentRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;

        public AdminStatisticsService(ArticleRepository articleRepository, 
                                      CommentRepository commentRepository, 
                                      CategoryRepository categoryRepository, 
                                      TagRepository tagRepository)
        {
            _articleRepository = articleRepository;
            _commentRepository = commentRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
        }

        public async Task<ApiResponse> GetStatisticsAsync()
        {
            var articles = await _articleRepository.GetArticlesAsync();
            var comments = await _commentRepository.GetCommentsAsync();
            var categories = await _categoryRepository.GetAllCategoriesAsync();
            var tags = await _tagRepository.GetAllTagsAsync();
            AdminStatistics adminStatistics = new AdminStatistics
            {
                TotalArticles = articles.Count,
                TotalComments = comments.Count,
                TotalCategories = categories.Count,
                TotalTags = tags.Count
            };

            // 统计最近7天每天的文章数量
            var articlesLast7Days = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                int count = articles.Count(a => a.CreatedAt.Date == date);
                articlesLast7Days.Add(count);
            }
            adminStatistics.ArticlesLast7Days = articlesLast7Days;

            // 获取浏览量最高的5篇文章
            var top5ArticlesByViews = articles.OrderByDescending(a => a.ViewCount).Take(5).ToList();
            var ArticlesByViewsDTOs = top5ArticlesByViews.Select(a => new ArticleSummaryDTO
            {
                Id = a.Id,
                Title = a.Title,
                ViewCount = a.ViewCount
            }).ToList();
            adminStatistics.Top5ArticlesByViews = ArticlesByViewsDTOs;

            return new ApiResponse(true, "数据统计成功！", adminStatistics);
        }
    }
}
