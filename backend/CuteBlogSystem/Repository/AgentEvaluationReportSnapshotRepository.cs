using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class AgentEvaluationReportSnapshotRepository
    {
        private readonly MyDbContext _dbContext;

        public AgentEvaluationReportSnapshotRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 根据 RunId 获取唯一评估报告快照
        public async Task<AgentEvaluationReportSnapshot?> GetSnapshotByRunIdAsync(long runId)
        {
            return await _dbContext.AgentEvaluationReportSnapshots
                .Where(snapshot => snapshot.RunId == runId && !snapshot.IsDeleted)
                .OrderByDescending(snapshot => snapshot.CreatedAt)
                .FirstOrDefaultAsync();
        }

        // 根据 RunId 判断快照是否存在
        public async Task<bool> ExistSnapshotByRunIdAsync(long runId)
        {
            return await _dbContext.AgentEvaluationReportSnapshots
                .AnyAsync(snapshot => snapshot.RunId == runId && !snapshot.IsDeleted);
        }

        // 添加新的评估报告快照（返回包含 id 的快照对象）
        public async Task<AgentEvaluationReportSnapshot?> AddSnapshotAsync(AgentEvaluationReportSnapshot snapshot)
        {
            await _dbContext.AgentEvaluationReportSnapshots.AddAsync(snapshot);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0 ? snapshot : null;
        }
    }
}
