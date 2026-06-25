using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Repository;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    public class AgentPendingConfirmationService
    {
        private static readonly TimeSpan ConfirmationExpireAfter = TimeSpan.FromMinutes(10);
        private readonly AgentPendingConfirmationRepository _confirmationRepository;
        private readonly ILogger<AgentPendingConfirmationService> _logger;

        public AgentPendingConfirmationService(
            AgentPendingConfirmationRepository confirmationRepository,
            ILogger<AgentPendingConfirmationService> logger)
        {
            _confirmationRepository = confirmationRepository;
            _logger = logger;
        }

        // 创建plan确认记录并保存到数据库
        public async Task<string?> CreateAsync(string sessionId, string userId, string userMessage, AgentPlan plan)
        {
            // 创建唯一性请求uid
            var confirmationId = $"confirm_{Guid.NewGuid():N}";

            // 记录记录保存时间
            var now = DateTime.UtcNow;

            // 创建待保存的实体
            var entity = new AgentPendingConfirmation
            {
                ConfirmationId = confirmationId,
                SessionId = sessionId,
                UserId = userId,
                UserMessage = userMessage,
                PlanJson = JsonSerializer.Serialize(plan),
                Status = AgentPendingConfirmationStatus.Pending,
                CreatedAt = now,
                ExpiresAt = now.Add(ConfirmationExpireAfter)
            };

            // 将实体保存进数据库
            var success = await _confirmationRepository.AddAsync(entity);

            // 如果成功返回请求id，失败则返回null
            return success ? confirmationId : null;
        }

        // 对某条待确认plan记录进行确认
        public async Task<AgentConfirmedPlan?> ConfirmAsync(string confirmationId, string userId, string sessionId)
        {
            // 根据 confirmationId 在数据库中搜索plan的确认记录
            var entity = await _confirmationRepository.GetByConfirmationId(confirmationId, userId, sessionId);

            // 如果未找到返回null
            if (entity == null)
            {
                return null;
            }

            // 如果该条记录不是待确认的状态返回null
            if (entity.Status != AgentPendingConfirmationStatus.Pending)
            {
                return null;
            }

            // 如果该条记录已经过期，将记录状态更改为过期并返回null
            if (entity.ExpiresAt < DateTime.UtcNow) 
            {
                entity.Status = AgentPendingConfirmationStatus.Expired;
                bool expiredSuccess = await _confirmationRepository.UpdateAsync(entity);
                if (!expiredSuccess)  // 过期处理失败则用日志记录，但不影响业务逻辑，更新成功与否都是返回null
                {
                    _logger.LogWarning("更新待确认请求为 Expired 失败。ConfirmationId: {ConfirmationId}", confirmationId);
                }
                return null;
            }

            // 将实体存储的计划json串反序列化为AgentPlan对象
            var plan = JsonSerializer.Deserialize<AgentPlan>(entity.PlanJson);

            // 如果反序列化失败，返回null
            if (plan == null)
            {
                return null;
            }

            // 将计划的状态更改为确认，并保存
            entity.Status = AgentPendingConfirmationStatus.Confirmed;
            entity.ConfirmedAt = DateTime.UtcNow;
            bool confirmSuccess = await _confirmationRepository.UpdateAsync(entity);

            if (!confirmSuccess)
            {
                return null;
            }
            else
            {
                return new AgentConfirmedPlan
                {
                    Plan = plan,
                    SessionId = sessionId,
                    UserId = userId,
                    UserMessage = entity.UserMessage
                };
            }
        }

        // 对某条待确认plan记录进行拒绝
        public async Task<bool> CancelAsync(string confirmationId, string userId, string sessionId)
        {
            // 根据 confirmationId 在数据库中搜索 plan 的确认记录
            var entity = await _confirmationRepository.GetByConfirmationId(confirmationId, userId, sessionId);

            // 如果未找到返回 false
            if (entity == null)
            {
                return false;
            }

            // 如果该条记录不是待确认的状态返回 false
            if (entity.Status != AgentPendingConfirmationStatus.Pending)
            {
                return false;
            }

            // 如果该条记录已经过期，将记录状态更改为过期并返回 false
            if (entity.ExpiresAt < DateTime.UtcNow)
            {
                entity.Status = AgentPendingConfirmationStatus.Expired;
                bool expiredSuccess = await _confirmationRepository.UpdateAsync(entity);
                if (!expiredSuccess)  // 过期处理失败则用日志记录，但不影响业务逻辑，更新成功与否都是返回null
                {
                    _logger.LogWarning("更新待确认请求为 Expired 失败。ConfirmationId: {ConfirmationId}", confirmationId);
                }
                return false;
            }

            // 对待确认记录进行拒绝状态更新，并保存到数据库
            entity.Status = AgentPendingConfirmationStatus.Cancelled;
            entity.CancelledAt = DateTime.UtcNow;

            return await _confirmationRepository.UpdateAsync(entity);
        }
    }
}
