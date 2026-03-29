using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class CommentRepository
    {
        private readonly MyDbContext _dbContext;
        private readonly ILogger<CommentRepository> _logger;

        public CommentRepository(MyDbContext dbContext, ILogger<CommentRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // 添加评论
        public async Task AddCommentAsync(Comment comment)
        {
            try
            {
                _dbContext.Comments.Add(comment);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加评论失败！ Comment: {@Comment}", comment);
                throw;
            }
        }

        // 根据id查询评论
        public async Task<Comment?> GetCommentByIdAsync(int commentId)
        {
            return await _dbContext.Comments.FindAsync(commentId);
        }

        // 根据评论ID查询评论是否存在
        public async Task<bool> GetCommentExistByIdAsync(int commentId)
        {
            return await _dbContext.Comments.FindAsync(commentId) != null;
        }

        // 根据评论id删除评论
        public async Task DeleteCommentByIdAsync(int commentId)
        {
            try
            {
                _dbContext.Comments.Remove(new Comment { Id = commentId });
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除评论失败！ CommentId: {CommentId}", commentId);
                throw;
            }
        }

        // 获取所有评论
        public async Task<List<Comment>> GetCommentsAsync()
        {
            return await _dbContext.Comments
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync<Comment>();
        }

        // 根据文章id查询评论列表
        public async Task<List<Comment>> GetCommentsByArticleIdAsync(int articleId)
        {
            return await _dbContext.Comments
                .Where(c => c.ArticleId == articleId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync<Comment>();
        }

        // 根据用户id查询评论列表
        public async Task<List<Comment>> GetCommentsByUserIdAsync(int userId)
        {
            return await _dbContext.Comments
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync<Comment>();
        }
    }
}
