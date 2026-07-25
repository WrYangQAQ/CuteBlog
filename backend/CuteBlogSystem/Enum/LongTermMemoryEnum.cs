namespace CuteBlogSystem.Enum
{
    // MemoryType 推荐常量
    public enum MemoryTypeConstants : byte
    {
        Unknown = 0,        // 未知
        Preference = 1,     // 用户偏好类记忆（如喜欢的分类、常用标签等）
        Fact = 2,           // 客观事实类记忆（如文章ID、分类名称等）
        Summary = 3,        // 摘要类记忆（如对话历史摘要、文章概要等）
        Episodic = 4,       // 情景事件类记忆（如用户操作记录、历史事件等）
        Instruction = 5     // 指令类记忆（如用户明确提出的操作要求）
    }

    // 记忆状态枚举（对应数据库 TINYINT）
    public enum MemoryStatus : byte
    {
        Unknown = 0,      // 未知
        Active = 1,       // 有效并参与检索
        Archived = 2,     // 已归档
        Superseded = 3,   // 已被替代
        Deleted = 4,      // 软删除
        Expired = 5       // 已过期
    }

    // SourceType 推荐常量
    public enum SourceTypeConstants : byte
    {
        Unknown = 0,                // 未知
        UserExplicit = 1,           // 用户明确提供的记忆（如用户主动声明的偏好）
        AgentInferred = 2,          // Agent 根据对话上下文推断出的记忆
        SystemDerived = 3,          // 系统通过内部逻辑推导出的记忆（如统计结果）
        ConversationSummary = 4,    // 从对话历史摘要中提取的记忆
        Imported = 5                // 从外部导入的记忆（如批量迁移）
    }

    // 记忆分组
    public enum MemoryGroupConstants : byte
    {
        Unknown = 0,
        UserPreference = 1,      // 用户偏好
        ArticleContext = 2,      // 文章上下文
        BlogOperation = 3,       // 博客操作习惯
        AgentBehaviour = 4,       // Agent 交互偏好
        ProjectLearning = 5,     // 当前项目学习进度
        ConversationContext = 6  // 跨会话对话上下文
    }

}
