using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class ArticleLikeRepository
    {
        private readonly MyDbContext _dbContext;
        private readonly ILogger<ArticleLikeRepository> _logger;

        public ArticleLikeRepository(MyDbContext dbContext, ILogger<ArticleLikeRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // 根据UserId和ArticleId查询点赞记录是否存在
        public async Task<bool> SearchArticleLikeExistAsync(int userId, int articleId)
        {
            try
            {
                var result = await _dbContext.ArticleLikes.FindAsync(articleId, userId);
                if (result == null)
                {
                    return false; // 没有点赞过
                }
                return true; // 已经点赞过
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询 UserId: {UserId} 对 ArticleId: {ArticleId} 的点赞记录失败！", userId, articleId);
                return false; // 查询失败，默认返回没有点赞过
            }
        }

        // 添加点赞记录
        public async Task AddArticleLikeAsync(int userId, int articleId)
        {
            try
            {
                ArticleLike articleLike = new ArticleLike
                {
                    UserId = userId,
                    ArticleId = articleId,
                    LikeAt = DateTime.UtcNow
                };
                _dbContext.ArticleLikes.Add(articleLike);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // 处理异常，记录日志
                _logger.LogError(ex, "对于 UserId: {UserId} 对 ArticleId: {ArticleId} 的点赞记录添加失败！", userId, articleId);

                // 抛出异常
                throw;
            }
        }

        // 删除点赞记录
        public async Task RemoveArticleLikeAsync(int userId, int articleId)
        {
            try
            {
                ArticleLike articleLike = await _dbContext.ArticleLikes.FindAsync(articleId, userId);
                if (articleLike != null)
                {
                    _dbContext.ArticleLikes.Remove(articleLike);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // 处理异常，记录日志
                _logger.LogError(ex, "对于 UserId: {UserId} 对 ArticleId: {ArticleId} 的点赞记录删除失败！", userId, articleId);

                // 抛出异常
                throw;
            }
        }

        // 根据文章ID查询点赞数量
        public async Task<int> GetArticleLikeCountAsync(int articleId)
        {
            return await _dbContext.ArticleLikes.CountAsync(al => al.ArticleId == articleId);
        }

        // 根据用户ID查询用户点赞的文章列表
        public async Task<List<Article>> GetLikedArticlesByUserIdAsync(int userId)
        {
            return await _dbContext.ArticleLikes
                .Where(al => al.UserId == userId)
                .Select(al => al.Article)
                .ToListAsync();
        }
    }
}
