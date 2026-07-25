using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.Agent
{
    // 用于更新长期记忆
    public class UpdateLongTermMemoryDto
    {
        // 记忆类型（如偏好、事实、摘要等）
        public MemoryTypeConstants MemoryType { get; set; }

        // 记忆分组（如对话、文章、系统等）
        public MemoryGroupConstants MemoryGroup { get; set; }

        // 业务键，用于唯一标识同一分组下的具体记忆条目
        public string MemoryKey { get; set; } = string.Empty;

        // 记忆的文本内容
        public string Content { get; set; } = string.Empty;

        // 记忆来源类型（用户显式、Agent推断、系统派生等）
        public SourceTypeConstants SourceType { get; set; }

        // 来源会话ID（可选）
        public string? SourceSessionId { get; set; }

        // 来源消息ID（可选）
        public long? SourceMessageId { get; set; }

        // 来源动作名称（可选）
        public string? SourceAction { get; set; }

        // 置信度（0~1，默认0.7）
        public decimal Confidence { get; set; } = 0.7m;

        // 重要性（0~1，默认0.5）
        public decimal Importance { get; set; } = 0.5m;

        // 是否固定（固定后不会被衰减）
        public bool IsPinned { get; set; } = false;

        // 过期时间（UTC，为空则永不过期）
        public DateTime? ExpiresAt { get; set; }

        // 额外元数据（JSON字符串）
        public string? MetadataJson { get; set; }
    }

    // LLM 提炼记忆后返回文本中 Json 反序列化类
    public class ExtractedMemoryItem
    {
        public string MemoryType { get; set; } = string.Empty;      // "Preference", "Fact", "Summary", etc.
        public string MemoryGroup { get; set; } = string.Empty;     // "UserPreference", "ArticleContext", etc.
        public string MemoryKey { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public decimal Confidence { get; set; } = 0.7m;
        public decimal Importance { get; set; } = 0.5m;
    }

    // 用户长期记忆遗忘指令的处理结果
    public sealed class ForgetLongTermMemoryResult
    {
        // 当前消息是否属于长期记忆遗忘指令
        public bool IsForgetCommand { get; set; }

        // 遗忘操作是否成功执行
        public bool Success { get; set; }

        // 是否因为目标不明确而需要用户补充说明
        public bool RequiresClarification { get; set; }

        // 本次被标记为Deleted的记忆数量
        public int DeletedCount { get; set; }

        // 供系统日志或AgentAskResponse.Message使用
        public string Message { get; set; } = string.Empty;

        // 最终返回给用户的回答
        public string Answer { get; set; } = string.Empty;
    }
}
