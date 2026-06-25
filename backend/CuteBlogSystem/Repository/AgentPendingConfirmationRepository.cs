using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class AgentPendingConfirmationRepository
    {
        private readonly MyDbContext _dbContext;

        public AgentPendingConfirmationRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 异步添加待确认记录到数据库，返回是否添加成功的布尔值
        public async Task<bool> AddAsync(AgentPendingConfirmation entity)
        {
            _dbContext.AgentPendingConfirmations.Add(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        // 根据确认请求Id查询对应Plan状态
        public async Task<AgentPendingConfirmation?> GetByConfirmationId(
            string confirmationId,
            string userId,
            string sessionId)
        {
            return await _dbContext.AgentPendingConfirmations
                .FirstOrDefaultAsync(x =>
                    x.ConfirmationId == confirmationId &&
                    x.UserId == userId &&
                    x.SessionId == sessionId);
        }

        // 更新plan的待确认记录，返回是否更新成功
        public async Task<bool> UpdateAsync(AgentPendingConfirmation entity)
        {
            _dbContext.AgentPendingConfirmations.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }
    }
}
