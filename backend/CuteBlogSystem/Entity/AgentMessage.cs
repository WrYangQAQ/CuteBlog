using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Entity
{
    public class AgentMessage
    {
        // 主键标识
        public long MessageId { get; set; }

        // 外键关联，指向AgentConversation的SessionId
        public string SessionId { get; set; } = string.Empty;

        // 消息角色，如"user"、"assistant"等
        public AgentMessageRole Role { get; set; } = AgentMessageRole.User;

        // 消息内容
        public string Content { get; set; } = string.Empty;

        // Token 消耗数量，可为 null
        public int? TokenCount { get; set; }

        // 消息创建时间
        public DateTime CreatedAt { get; set; }

        // 导航属性，关联到AgentConversation
        public AgentConversation? Conversation { get; set; }
    }
}