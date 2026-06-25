using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class AgentWorkflowLogRepository
    {
        private readonly MyDbContext _dbContext;

        public AgentWorkflowLogRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 查询所有日志记录
        public async Task<List<AgentWorkflowLog>> GetAllLogsAsync()
        {
            var logs = await _dbContext.AgentWorkflowLogs
                .OrderByDescending(log => log.Id)
                .ToListAsync();
            return logs;
        }

        // 根据id查询单条日志记录
        public async Task<AgentWorkflowLog?> GetLogByIdAsync(int id)
        {
            var log = await _dbContext.AgentWorkflowLogs.FindAsync(id);
            return log;
        }

        // 添加新的日志记录
        public async Task<bool> AddLogAsync(AgentWorkflowLog log)
        {
            _dbContext.AgentWorkflowLogs.Add(log);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0;
        }

        // 查询指定时间范围内的日志记录
        public async Task<List<AgentWorkflowLog>> GetLogsByTimeRangeAsync(DateTime from, DateTime to)
        {
            var logs = await _dbContext.AgentWorkflowLogs
                .Where(log => log.StartedAt >= from && log.FinishedAt <= to)
                .OrderByDescending(log => log.Id)
                .ToListAsync();
            return logs;
        }

        // 取出最近的 count 条日志记录
        public async Task<List<AgentWorkflowLog>> GetRecentAsync(int count = 20)
        {
            return await _dbContext.AgentWorkflowLogs
                .OrderByDescending(x => x.Id)
                .Take(count)
                .ToListAsync();
        }
    }
}
