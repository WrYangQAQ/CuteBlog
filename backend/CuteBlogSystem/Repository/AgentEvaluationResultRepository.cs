using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class AgentEvaluationResultRepository
    {
        private readonly MyDbContext _dbContext;

        public AgentEvaluationResultRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 批量添加评估测试结果
        public async Task<bool> AddRangeAsync(List<AgentEvaluationResult> results)
        {
            await _dbContext.AgentEvaluationResults.AddRangeAsync(results);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return affectedRows > 0;
        }

        // 根据Case Id查询对应执行结果列表
        public async Task<List<AgentEvaluationResult>> FindResultByCaseId(int caseId)
        {
            var results = await _dbContext.AgentEvaluationResults
                .Where(c => c.TestCaseId == caseId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            return results;
        }

        // 根据Run Id查询对应执行结果列表
        public async Task<List<AgentEvaluationResult>> FindResultByRunId(long runId)
        {
            var results = await _dbContext.AgentEvaluationResults
                .Where(c => c.RunId == runId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            return results;
        }

        // 根据runId和caseId联合查询对应执行记录
        public async Task<AgentEvaluationResult?> FindResultByRunIdAndCaseIdAsync(long runId, int caseId)
        {
            var result = await _dbContext.AgentEvaluationResults
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.RunId == runId && c.TestCaseId == caseId);
            return result;
        }
    }
}
