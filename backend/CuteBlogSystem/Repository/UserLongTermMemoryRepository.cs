using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    // Agent 跨会话长期记忆的数据访问层
    // 用户长期记忆的数据访问层，负责增删改查及衰减相关操作
    public class UserLongTermMemoryRepository
    {
        private readonly MyDbContext _dbContext;

        public UserLongTermMemoryRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 通过记忆ID和用户ID获取单条长期记忆
        public async Task<UserLongTermMemory?> GetByIdAsync(Guid memoryId, int userId)
        {
            return await _dbContext.UserLongTermMemories
                .FirstOrDefaultAsync(memory => memory.MemoryId == memoryId && memory.UserId == userId);
        }

        // 通过用户ID、记忆类型、分组和业务键获取单条有效记忆
        public async Task<UserLongTermMemory?> GetActiveByKeyAsync(
            int userId,
            MemoryTypeConstants memoryType,
            MemoryGroupConstants? memoryGroup,
            string memoryKey)
        {
            return await _dbContext.UserLongTermMemories
                .FirstOrDefaultAsync(memory =>
                    memory.UserId == userId &&
                    memory.Status == MemoryStatus.Active &&
                    memory.MemoryType == memoryType &&
                    memory.MemoryGroup == memoryGroup &&
                    memory.MemoryKey == memoryKey);
        }

        // 获取用户活跃记忆列表，支持按类型和分组过滤，按置顶、重要度、置信度等排序
        public async Task<List<UserLongTermMemory>> GetActiveMemoriesAsync(
            int userId,
            int limit = 20,
            MemoryTypeConstants? memoryType = null,
            MemoryGroupConstants? memoryGroup = null)
        {
            var now = DateTime.UtcNow;

            var query = _dbContext.UserLongTermMemories
                .Where(memory =>
                    memory.UserId == userId &&
                    memory.Status == MemoryStatus.Active &&
                    (memory.ExpiresAt == null || memory.ExpiresAt > now));

            if (memoryType.HasValue)
            {
                query = query.Where(memory => memory.MemoryType == memoryType.Value);
            }

            if (memoryGroup != null && memoryGroup != MemoryGroupConstants.Unknown)
            {
                query = query.Where(memory => memory.MemoryGroup == memoryGroup);
            }

            return await query
                .OrderByDescending(memory => memory.IsPinned)
                .ThenByDescending(memory => memory.Importance)
                .ThenByDescending(memory => memory.Confidence)
                .ThenByDescending(memory => memory.LastAccessedAt ?? memory.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // 在活跃记忆中按关键词搜索（匹配 Content、MemoryKey）
        public async Task<List<UserLongTermMemory>> SearchActiveMemoriesAsync(int userId, string keyword, int limit = 20)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(keyword) || limit <= 0)
            {
                return new List<UserLongTermMemory>();
            }

            var now = DateTime.UtcNow;
            var normalizedKeyword = keyword.Trim();

            return await _dbContext.UserLongTermMemories
                .Where(memory =>
                    memory.UserId == userId &&
                    memory.Status == MemoryStatus.Active &&
                    (memory.ExpiresAt == null || memory.ExpiresAt > now) &&
                    (memory.Content.Contains(normalizedKeyword) ||
                     (memory.MemoryKey != null && memory.MemoryKey.Contains(normalizedKeyword))
                     ))
                .OrderByDescending(memory => memory.IsPinned)
                .ThenByDescending(memory => memory.Importance)
                .ThenByDescending(memory => memory.Confidence)
                .ThenByDescending(memory => memory.LastAccessedAt ?? memory.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // 获取达到衰减时间的活跃记忆
        public async Task<List<UserLongTermMemory>> GetMemoriesForDecayAsync(DateTime decayBefore, DateTime now, int limit = 100)
        {
            if (limit <= 0)
            {
                return new List<UserLongTermMemory>();
            }

            return await _dbContext.UserLongTermMemories
                .Where(memory =>
                    memory.Status == MemoryStatus.Active &&
                    !memory.IsPinned &&

                    // 已经过期的记忆不再执行置信度衰减
                    (memory.ExpiresAt == null ||
                     memory.ExpiresAt > now) &&

                    // 创建、确认和上次衰减都至少已经过去一天
                    memory.CreatedAt <= decayBefore &&
                    (memory.LastConfirmedAt == null ||
                     memory.LastConfirmedAt <= decayBefore) &&
                    (memory.LastDecayAt == null ||
                     memory.LastDecayAt <= decayBefore))
                .OrderBy(memory =>
                    memory.LastDecayAt ??
                    memory.LastConfirmedAt ??
                    memory.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // 添加一条新记忆
        public async Task<UserLongTermMemory?> AddAsync(UserLongTermMemory memory)
        {
            _dbContext.UserLongTermMemories.Add(memory);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0 ? memory : null;
        }

        // 更新单条记忆，发生并发冲突时返回 false
        public async Task<UserLongTermMemory?> UpdateAsync(UserLongTermMemory memory)
        {
            try
            {
                _dbContext.UserLongTermMemories.Update(memory);
                var affectedRows = await _dbContext.SaveChangesAsync();
                return affectedRows > 0 ? memory : null;
            }
            catch (DbUpdateConcurrencyException)
            {
                return null;
            }
        }

        // 批量更新记忆，发生并发冲突时返回 null
        public async Task<List<UserLongTermMemory>?> UpdateRangeAsync(List<UserLongTermMemory> memories)
        {
            if (memories.Count == 0)
            {
                return null;
            }

            try
            {
                _dbContext.UserLongTermMemories.UpdateRange(memories);
                var affectedRows = await _dbContext.SaveChangesAsync();
                return affectedRows > 0 ? memories : null;
            }
            catch (DbUpdateConcurrencyException)
            {
                return null;
            }
        }

        // 将旧记忆标记为已被替代，并原子性地添加新版本
        public async Task<UserLongTermMemory?> SupersedeAndAddAsync(
            UserLongTermMemory existingMemory,
            UserLongTermMemory newMemory)
        {
            try
            {
                // 旧版本更新和新版本添加必须在同一次SaveChanges中提交
                _dbContext.UserLongTermMemories.Update(existingMemory);

                _dbContext.UserLongTermMemories.Add(newMemory);

                var affectedRows = await _dbContext.SaveChangesAsync();

                // 正常情况下至少影响旧记录和新记录两条数据
                return affectedRows >= 2 ? newMemory : null;
            }
            catch (DbUpdateConcurrencyException)
            {
                // 清理失败操作在当前DbContext中的跟踪状态
                _dbContext.Entry(existingMemory).State = EntityState.Detached;

                _dbContext.Entry(newMemory).State = EntityState.Detached;

                return null;
            }
            catch
            {
                // SaveChanges失败时数据库事务会自动回滚，
                // 同时清理当前上下文中的失败实体
                _dbContext.Entry(existingMemory).State = EntityState.Detached;

                _dbContext.Entry(newMemory).State = EntityState.Detached;

                throw;
            }
        }

        // 获取已经到期但状态仍为Active的长期记忆
        public async Task<List<UserLongTermMemory>> GetExpiredActiveMemoriesAsync(DateTime now, int limit = 100)
        {
            if (limit <= 0)
            {
                return new List<UserLongTermMemory>();
            }

            return await _dbContext.UserLongTermMemories
                .Where(memory =>
                    memory.Status == MemoryStatus.Active &&
                    !memory.IsPinned &&
                    memory.ExpiresAt.HasValue &&
                    memory.ExpiresAt.Value <= now)
                .OrderBy(memory => memory.ExpiresAt)
                .Take(limit)
                .ToListAsync();
        }

        // 获取需要自动归档的低置信度、长期未活动记忆
        public async Task<List<UserLongTermMemory>> GetMemoriesForArchiveAsync(
            DateTime now,
            DateTime idleBefore,
            decimal confidenceThreshold,
            int limit = 100)
        {
            if (limit <= 0)
            {
                return new List<UserLongTermMemory>();
            }

            return await _dbContext.UserLongTermMemories
                .Where(memory =>
                    memory.Status == MemoryStatus.Active &&
                    !memory.IsPinned &&

                    // 已到期的记忆应该进入 Expired，不在这里归档
                    (memory.ExpiresAt == null || memory.ExpiresAt > now) &&

                    // 置信度足够低
                    memory.Confidence <= confidenceThreshold &&

                    // 创建、访问、确认均已超过闲置期限
                    memory.CreatedAt <= idleBefore &&
                    (memory.LastAccessedAt == null ||
                     memory.LastAccessedAt <= idleBefore) &&
                    (memory.LastConfirmedAt == null ||
                     memory.LastConfirmedAt <= idleBefore))
                .OrderBy(memory => memory.Confidence)
                .ThenBy(memory => memory.LastAccessedAt ?? memory.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // 获取超过保留期限、需要软删除的非活跃记忆
        public async Task<List<UserLongTermMemory>> GetMemoriesForSoftDeleteAsync(DateTime retentionBefore, int limit = 100)
        {
            if (limit <= 0)
            {
                return new List<UserLongTermMemory>();
            }

            return await _dbContext.UserLongTermMemories
                .Where(memory =>
                    // 过期记忆以 ExpiresAt 作为进入非活跃状态的时间
                    (memory.Status == MemoryStatus.Expired &&
                     memory.ExpiresAt.HasValue &&
                     memory.ExpiresAt.Value <= retentionBefore) ||

                    // 归档和被替代记忆以 ArchivedAt 作为状态变更时间
                    ((memory.Status == MemoryStatus.Archived ||
                      memory.Status == MemoryStatus.Superseded) &&
                     memory.ArchivedAt.HasValue &&
                     memory.ArchivedAt.Value <= retentionBefore))
                .OrderBy(memory =>
                    memory.ArchivedAt ??
                    memory.ExpiresAt ??
                    memory.UpdatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // 获取用户需要参与遗忘处理的活跃长期记忆
        public async Task<List<UserLongTermMemory>> GetActiveMemoriesForForgetAsync(int userId, int limit = 100)
        {
            if (userId <= 0 || limit <= 0)
            {
                return new List<UserLongTermMemory>();
            }

            return await _dbContext.UserLongTermMemories
                .Where(memory => memory.UserId == userId && memory.Status == MemoryStatus.Active)
                .OrderByDescending(memory => memory.IsPinned)
                .ThenByDescending(memory => memory.Importance)
                .ThenByDescending(memory => memory.UpdatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
