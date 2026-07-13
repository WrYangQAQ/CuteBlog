using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class AgentEvaluationRunRepository
    {
        private readonly MyDbContext _dbContext;

        public AgentEvaluationRunRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 添加评估测试批次记录
        public async Task<AgentEvaluationRun?> AddAsync(AgentEvaluationRun evaluationRun)
        {
            await _dbContext.AgentEvaluationRuns.AddAsync(evaluationRun);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0 ? evaluationRun : null;
        }

        // 评估结束后存入评估结果到评估批次记录中，并保存记录
        public async Task<bool> FinishAsync(long runId, int passedCount, int failedCount, DateTime finishedAt, string? remark)
        {
            var evaluationRun = await _dbContext.AgentEvaluationRuns.FindAsync(runId);

            if (evaluationRun == null)
            {
                return false;
            }
            evaluationRun.PassedCount = passedCount;
            evaluationRun.FailedCount = failedCount;
            evaluationRun.FinishedAt = finishedAt;
            if (remark != null)
            {
                evaluationRun.Remark = remark;
            }
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0;
        }

        // 查询最近 count 条评估测试批次记录
        public async Task<List<AgentEvaluationRun>> GetRecentAsync(int count)
        {
            var runs = await _dbContext.AgentEvaluationRuns
                .OrderByDescending(run => run.FinishedAt)
                .Take(count)
                .ToListAsync();
            return runs;
        }

        // 根据 Id 查询某一条批次记录
        public async Task<AgentEvaluationRun?> GetByIdAsync(long runId)
        {
            return await _dbContext.AgentEvaluationRuns.FindAsync(runId);
        }
    }
}
