namespace CuteBlogSystem.Entity
{
    // 用于存储 Agent 与用户之间的对话记忆，以便在后续交互中提供上下文
    public class AgentConversationMemory
    {
        // 记录的唯一标识符
        public int Id { get; set; }

        // 会话标识符，用于区分不同的对话会话（如用户ID + 会话Token）
        public string SessionId { get; set; } = string.Empty;

        // 最近一次用户发送的消息内容
        public string LastUserMessage { get; set; } = string.Empty;

        // 最近一次 Agent 返回给用户的答案
        public string LastAnswer { get; set; } = string.Empty;

        // 最后被用户选中或讨论的文章ID（可为空）
        public int? LastSelectedArticleId { get; set; }

        // 最后被选中文章的文章标题（可为空）
        public string? LastSelectedArticleTitle { get; set; }

        // 该会话记忆的创建时间
        public DateTime CreatedAt { get; set; }

        // 该会话记忆的最后更新时间（每次更新记录时刷新）
        public DateTime UpdatedAt { get; set; }
        
        // 较早之前的对话消息的摘要信息
        public string? ConversationSummary { get; set; }

        // 已经被摘要覆盖的最后一条消息ID
        public long? LastSummarizedMessageId { get; set; }

        // 摘要最后一次更新的事件
        public DateTime? SummaryLastUpdate { get; set; }

        // 重置上下文时对应的消息ID，只使用该条消息之后的消息
        public long? ContextResetMessageId { get; set; }

        // 最近一次重置上下文的时间
        public DateTime? ContextResetAt { get; set; }

        // 最近上下文中提到的文章列表序列化的json
        public string? RecentMentionedArticlesJson { get; set; }

        // 导航属性，关联到 AgentConversation
        public AgentConversation? Conversation { get; set; }
    }
}