using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    // 负责 Agent 对话记忆的数据访问操作（增、查、改）
    public class AgentConversationMemoryRepository
    {
        private readonly MyDbContext _dbContext;   // 数据库上下文，用于执行 EF Core 操作

        public AgentConversationMemoryRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 根据会话 ID 异步查询对话记忆记录，若不存在则返回 null
        public async Task<AgentConversationMemory?> GetByConversationIdAsync(string conversationId)
        {
            return await _dbContext.AgentConversationMemories
                .FirstOrDefaultAsync(m => m.SessionId == conversationId);
        }

        // 异步添加新的对话记忆记录到数据库，并返回添加后的实体
        public async Task<AgentConversationMemory> AddAsync(AgentConversationMemory memory)
        {
            _dbContext.AgentConversationMemories.Add(memory);
            await _dbContext.SaveChangesAsync();
            return memory;
        }

        // 异步更新现有的对话记忆记录，返回是否成功（影响行数大于0）
        public async Task<bool> UpdateAsync(AgentConversationMemory memory)
        {
            _dbContext.AgentConversationMemories.Update(memory);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0;
        }
    }
}