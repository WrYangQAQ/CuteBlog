using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    // 负责 Agent 消息记录的数据访问操作
    public class AgentMessageRepository
    {
        private readonly MyDbContext _dbContext;   // 数据库上下文

        public AgentMessageRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 异步添加新的消息记录到数据库，并返回成功与否的布尔值
        public async Task<bool> AddMessageAsync(AgentMessage message)
        {
            _dbContext.AgentMessages.Add(message);
            int affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0;
        }

        // 根据会话 ID 异步获取该会话下的所有消息，按创建时间升序排列
        public async Task<List<AgentMessage>> GetBySessionIdAsync(string sessionId)
        {
            return await _dbContext.AgentMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        // 根据消息 ID 异步与数量获取固定数量的消息记录
        public async Task<List<AgentMessage>> GetRecentBySessionIdAsync(
            string sessionId, 
            int count, 
            long? beforeMessageId = null,
            long? afterMessageId = null)
        {
            var query = _dbContext.AgentMessages
                .Where(m => m.SessionId == sessionId);   // 查询该条会话下的所有消息记录

            if (beforeMessageId.HasValue)
            {
                query = query.Where(m => m.MessageId < beforeMessageId.Value);
            }

            if (afterMessageId.HasValue)
            {
                query = query.Where(
                    message => message.MessageId > afterMessageId.Value);
            }

            return await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        // 计算有多少条对话信息是未被总结的
        public async Task<int> CountUnsummarizedMessagesAsync(string sessionId, long? lastSummarizedMessageId)
        {
            var query = _dbContext.AgentMessages
                .Where(message => message.SessionId == sessionId);

            if (lastSummarizedMessageId.HasValue)
            {
                query = query.Where(message => message.MessageId > lastSummarizedMessageId.Value);
            }

            var result = await query.CountAsync();
            return result;
        }

        // 获取待进行摘要总结的信息列表
        public async Task<List<AgentMessage>> GetMessagesForSummaryAsync(
            string sessionId,
            long? lastSummarizedMessageId,
            int keepRecentCount,
            int batchSize
        )
        {
            var query = _dbContext.AgentMessages
                .Where(message => message.SessionId == sessionId);   // 取出该会话下的所有消息

            if (lastSummarizedMessageId.HasValue)
            {
                query = query.Where(
                    message => message.MessageId > lastSummarizedMessageId.Value);    // 在集合中过滤出所有未经过摘要总结的消息
            }

            // 找到需要保留的最近消息之前的边界。
            var summaryBoundaryId = await query
                .OrderByDescending(message => message.MessageId)    // 通过主键索引（数据库走B+树）定位边界ID
                .Skip(keepRecentCount)
                .Select(message => (long?)message.MessageId)
                .FirstOrDefaultAsync();

            if (!summaryBoundaryId.HasValue)
            {
                return new List<AgentMessage>();   // 没有值则返回空集合
            }

            return await query
                .Where(message => message.MessageId <= summaryBoundaryId.Value)
                .OrderBy(message => message.MessageId)
                .Take(batchSize)
                .ToListAsync();
        }
    }
}