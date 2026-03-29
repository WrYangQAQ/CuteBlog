using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class ArticleTagRepository
    {
        private readonly MyDbContext _dbContext;

        public ArticleTagRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 根据文章ID查询标签列表
        public async Task<List<int>> GetTagIdsByArticleIdAsync(int articleId)
        {
            var tagIds = await _dbContext.ArticleTags
                .Where(at => at.ArticleId == articleId)
                .Select(at => at.TagId)
                .ToListAsync();
            return tagIds;
        }

        // 根据标签ID查询文章列表
        public async Task<List<int>> GetArticleIdsByTagIdAsync(int tagId)
        {
            var articleIds = await _dbContext.ArticleTags
                .Where(at => at.TagId == tagId)
                .Select(at => at.ArticleId)
                .ToListAsync();
            return articleIds;
        }

        // 根据标签ID与文章ID删除记录
        public async Task DeleteArticleTagAsync(int articleId, int tagId)
        {
            var articleTag = await _dbContext.ArticleTags
                .FirstOrDefaultAsync(at => at.ArticleId == articleId && at.TagId == tagId);
            if (articleTag != null)
            {
                _dbContext.ArticleTags.Remove(articleTag);
                await _dbContext.SaveChangesAsync();
            }
        }

        //根据标签ID与文章ID添加记录
        public async Task AddArticleTagAsync(int articleId, int tagId)
        {
            ArticleTag articleTag = new ArticleTag
            {
                ArticleId = articleId,
                TagId = tagId
            };
            _dbContext.ArticleTags.Add(articleTag);
            await _dbContext.SaveChangesAsync();
        }
    }
}
