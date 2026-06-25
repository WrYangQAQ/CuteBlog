using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    // 负责 Agent 对话记录的数据访问操作（增、查、改）
    public class AgentConversationRepository
    {
        private readonly MyDbContext _dbContext;   // 数据库上下文

        public AgentConversationRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 根据会话 ID 异步查询对话记录，若不存在则返回 null
        public async Task<AgentConversation?> GetBySessionIdAsync(string sessionId)
        {
            return await _dbContext.AgentConversations
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        // 异步添加新的对话记录到数据库，并返回成功与否的布尔值
        public async Task<bool> AddConversationAsync(AgentConversation conversation)
        {
            _dbContext.AgentConversations.Add(conversation);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0;
        }

        // 异步更新现有的对话记录，返回是否成功（影响行数大于0）
        public async Task<bool> UpdateAsync(AgentConversation conversation)
        {
            _dbContext.AgentConversations.Update(conversation);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0;
        }

        // 获取最近的对话记录列表（可选按用户ID过滤，默认取20条，仅状态为 Active）
        public async Task<List<AgentConversation>> GetRecentAsync(int? userId, int take = 20)
        {
            var query = _dbContext.AgentConversations.AsQueryable();

            if (userId != null)
            {
                query = query.Where(c => c.UserId == userId);
            }

            return await query
                .Where(c => c.Status == AgentConversationStatus.Active)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(take)
                .ToListAsync();
        }

        // 根据 userId 获取所有对话记录列表（仅状态为 Active）
        public async Task<List<AgentConversation>> GetConversationsByUserIdAsync(int userId)
        {
            return await _dbContext.AgentConversations
                .Where(c => c.UserId == userId && c.Status == AgentConversationStatus.Active)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        // 根据会话 ID 异步删除对话记录（逻辑删除：将状态改为 Deleted），返回是否成功
        public async Task<bool> DeleteBySessionIdAsync(string sessionId)
        {
            var conversation = await GetBySessionIdAsync(sessionId);
            if (conversation == null)
            {
                return false;   // 未找到记录，删除失败
            }
            conversation.Status = AgentConversationStatus.Deleted;   // 逻辑删除
            _dbContext.AgentConversations.Update(conversation);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0;
        }

        // 根据会话 ID 对会话状态进行处理
        public async Task<bool> UpdateStatusAsync(
            string sessionId,
            AgentConversationStatus status)
        {
            var session = await GetBySessionIdAsync(sessionId);

            if (session == null)
            {
                return false;
            }

            session.Status = status;
            session.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(session);
        }

        // 查询已经归档的会话，返回会话列表
        public async Task<List<AgentConversation>> GetArchivedConversationsByUserIdAsync(int userId)
        {
            return await _dbContext.AgentConversations
                .Where(conversation => 
                    conversation.UserId == userId
                    && conversation.Status == AgentConversationStatus.Archived)
                .OrderByDescending(conversation => conversation.UpdatedAt)
                .ToListAsync();
        }
    }
}