using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace CuteBlogSystem.Repository
{
    public class ArticleRepository
    {
        private readonly ILogger<ArticleRepository> _logger;
        private readonly MyDbContext _dbContext;

        public ArticleRepository(MyDbContext dbContext, ILogger<ArticleRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // 查询文章列表
        public async Task<List<Article>> GetArticlesAsync()
        {
            var articles = await _dbContext.Articles
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .ToListAsync();
            return articles;
        }

        // 根据文章ID查询文章
        public async Task<Article> GetArticleByIdAsync(int id)
        {
            var article = await _dbContext.Articles
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .FirstOrDefaultAsync(a => a.Id == id);
            return article;
        }

        // 更新文章
        public async Task UpdateArticleAsync(Article article)
        {
            _dbContext.Articles.Update(article);
            await _dbContext.SaveChangesAsync();
        }

        // 添加文章
        public async Task AddArticleAsync(Article article)
        {
            _dbContext.Articles.Add(article);
            await _dbContext.SaveChangesAsync();
        }

        // 删除文章
        public async Task DeleteArticleByIdAsync(int articleId)
        {
            _dbContext.Articles.Remove(new Article { Id = articleId });
            await _dbContext.SaveChangesAsync();
        }

        // 获取置顶文章列表
        public async Task<List<Article>> GetTopArticlesAsync()
        {
            return await _dbContext.Articles
                .Where(a => a.IsTop)
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .ToListAsync();
        }

        // 获取推荐文章列表
        public async Task<List<Article>> GetRecommendedArticlesAsync()
        {
            return await _dbContext.Articles
                .Where(a => a.IsRecommend)
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .ToListAsync();
        }

        // 根据用户ID查询文章列表
        public async Task<List<Article>> GetArticlesByUserIdAsync(int userId)
        {
            return await _dbContext.Articles
                .Where(a => a.UserId == userId)
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .ToListAsync();
        }

        // 根据文章id检测是否存在
        public async Task<bool> ArticleExistsByIdAsync(int articleId)
        {
            return await _dbContext.Articles.AnyAsync(a => a.Id == articleId);
        }

        // 根据 SerachArticleDTO 查询文章列表
        public async Task<List<Article>> SearchArticlesAsync(string? keyword, List<string>? articleTags, string? category)
        {
            var query = _dbContext.Articles
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .AsQueryable();

            // 根据关键词、标签和分类进行过滤
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(a => a.Title.Contains(keyword) || a.Content.Contains(keyword));
            }
            if (articleTags != null && articleTags.Count > 0)
            {
                query = query.Where(a => a.ArticleTags.Any(at => articleTags.Contains(at.Tag.Name)));
            }
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.Category.Name == category);
            }
            return await query.ToListAsync();
        }
    }
}
