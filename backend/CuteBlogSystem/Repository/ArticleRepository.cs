using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
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
        public async Task<Article?> GetArticleByIdAsync(int id)
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

        // 根据 SearchArticleDTO 查询文章列表
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

        // 获取最新发布的五篇文章
        public async Task<List<Article>> GetLatestArticlesAsync()
        {
            return await _dbContext.Articles
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
                .ToListAsync();
        }

        // 根据分类Id查询该分类下的所有文章列表
        public async Task<List<Article>> GetArticlesByCategoryAsync(int categoryId)
        {
            return await _dbContext.Articles
                .Where(a => a.Category.Id == categoryId)
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .ToListAsync();
        }

        // 根据ID列表获取对应的文章列表
        public async Task<List<Article>> GetArticlesByIdsAsync(List<int> articleIds)
        {
            return await _dbContext.Articles
                .Where(a => articleIds.Contains(a.Id))
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .ToListAsync();
        }

        // 根据文章标题搜索文章
        public async Task<List<Article>> GetArticlesByTitleAsync(string title, bool onlyMine, int? userId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return new List<Article>();
            }

            title = title.Trim();

            var query = _dbContext.Articles
                .Where(article => article.Title.Contains(title))
                .AsQueryable();

            if (onlyMine)
            {
                if (userId is not int currentUserId || currentUserId <= 0)
                {
                    return new List<Article>();
                }

                query = query.Where(article => article.UserId == currentUserId);
            }

            return await query
                .Include(article => article.Category)
                .Include(article => article.User)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .ToListAsync();
        }

        // 根据分类名称搜索文章
        public async Task<List<Article>> GetArticlesByCategoryNameAsync(string categoryName, bool onlyMine, int? userId)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return new List<Article>();
            }

            categoryName = categoryName.Trim();

            var query = _dbContext.Articles
                .Where(article => article.Category.Name.Contains(categoryName))
                .AsQueryable();

            if (onlyMine)
            {
                if (userId is not int currentUserId || currentUserId <= 0)
                {
                    return new List<Article>();
                }

                query = query.Where(article => article.UserId == currentUserId);
            }

            return await query
                .Include(article => article.Category)
                .Include(article => article.User)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .ToListAsync();
        }

        // 根据文章正文内容或摘要搜索文章
        public async Task<List<Article>> GetArticlesByContentAsync(string content, bool onlyMine, int? userId)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<Article>();
            }

            content = content.Trim();

            var query = _dbContext.Articles
                .Where(article =>
                    article.Content.Contains(content) ||
                    article.Summary.Contains(content))
                .AsQueryable();

            if (onlyMine)
            {
                if (userId is not int currentUserId || currentUserId <= 0)
                {
                    return new List<Article>();
                }

                query = query.Where(article => article.UserId == currentUserId);
            }

            return await query
                .Include(article => article.Category)
                .Include(article => article.User)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .ToListAsync();
        }

        // 根据标题、分类名称、标签名称、正文内容或摘要综合搜索文章
        public async Task<List<Article>> GetArticlesByQueryTextAsync(string queryText, bool onlyMine, int? userId)
        {
            if (string.IsNullOrWhiteSpace(queryText))
            {
                return new List<Article>();
            }

            queryText = queryText.Trim();

            var query = _dbContext.Articles
                .Where(article =>
                    article.Title.Contains(queryText) ||
                    article.Category.Name.Contains(queryText) ||
                    article.Content.Contains(queryText) ||
                    article.Summary.Contains(queryText) ||
                    article.ArticleTags.Any(articleTag =>
                        articleTag.Tag.Name.Contains(queryText)))
                .AsQueryable();

            if (onlyMine)
            {
                if (userId is not int currentUserId || currentUserId <= 0)
                {
                    return new List<Article>();
                }

                query = query.Where(article => article.UserId == currentUserId);
            }

            return await query
                .Include(article => article.Category)
                .Include(article => article.User)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .ToListAsync();
        }

        // 根据标签名称搜索文章
        public async Task<List<Article>> GetArticlesByTagNameAsync(string tagName, bool onlyMine, int? userId)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return new List<Article>();
            }

            tagName = tagName.Trim();

            var query = _dbContext.Articles
                .Where(article =>
                    article.ArticleTags.Any(articleTag =>
                        articleTag.Tag.Name.Contains(tagName)))
                .AsQueryable();

            if (onlyMine)
            {
                if (userId is not int currentUserId || currentUserId <= 0)
                {
                    return new List<Article>();
                }

                query = query.Where(article => article.UserId == currentUserId);
            }

            return await query
                .Include(article => article.Category)
                .Include(article => article.User)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .ToListAsync();
        }

        // 根据标签ID查询对应的文章列表
        public async Task<List<Article>> GetArticlesByTagIdAsync(int tagId)
        {
            if (tagId <= 0)
            {
                return new List<Article>();
            }

            return await _dbContext.Articles
                .Where(article =>
                    article.ArticleTags.Any(articleTag =>
                        articleTag.TagId == tagId))
                .Include(article => article.Category)
                .Include(article => article.User)
                .Include(article => article.ArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .ToListAsync();
        }
    }
}
