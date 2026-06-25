using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Entity
{
    public class AgentPendingConfirmation
    {
        // 记录的唯一标识符
        public long Id { get; set; }

        // 确认请求的唯一标识符，用于外部引用
        public string ConfirmationId { get; set; } = string.Empty;

        // 会话标识符，用于关联对话上下文
        public string SessionId { get; set; } = string.Empty;

        // 用户标识符
        public string UserId { get; set; } = string.Empty;

        // 用户原始消息内容
        public string UserMessage { get; set; } = string.Empty;

        // 待确认的计划 JSON 字符串
        public string PlanJson { get; set; } = string.Empty;

        // 确认状态：待处理、已确认、已取消、已过期等
        public AgentPendingConfirmationStatus Status { get; set; } = AgentPendingConfirmationStatus.Pending;

        // 记录创建时间（UTC）
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 过期时间（UTC），超时未确认则自动取消
        public DateTime ExpiresAt { get; set; }

        // 确认时间（UTC），用户确认时记录
        public DateTime? ConfirmedAt { get; set; }

        // 取消时间（UTC），用户取消或自动过期时记录
        public DateTime? CancelledAt { get; set; }
    }
}
