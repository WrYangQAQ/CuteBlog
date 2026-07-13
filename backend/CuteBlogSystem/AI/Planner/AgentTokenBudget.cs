namespace CuteBlogSystem.AI.Planner
{
    // 集中管理 Agent 系统中与 Token 预算相关的常量，防止硬编码并便于统一调整
    public static class AgentTokenBudget
    {
        // 用户消息允许的最大字符数（超过可能触发截断或拒绝）
        public const int MaxUserMessageChars = 2000;

        // 规划器（Planner）生成计划时允许的最大输出 Token 数
        public const int PlannerMaxOutputTokens = 1200;

        // 计划修复服务生成修复计划时允许的最大输出 Token 数
        public const int PlanRepairMaxOutputTokens = 1200;

        // 失败分析服务生成失败分析时允许的最大输出 Token 数
        public const int FailureAnalysisMaxOutputTokens = 900;

        // 重新规划服务生成补救计划时允许的最大输出 Token 数
        public const int ReplannerMaxOutputTokens = 900;

        // 文章总结时允许的最大输出 Token 数
        public const int SummaryMaxOutputTokens = 1400;

        // 文章对比分析时允许的最大输出 Token 数
        public const int CompareMaxOutputTokens = 1800;

        // 生成补救建议时允许的最大输出 Token 数
        public const int RecoverySuggestionMaxOutputTokens = 1200;

        // 构建上下文时预留的 Token 数量，以确保系统回复时不会超出模型限制
        public const int RecentConversationMaxChars = 3000;

        // 最终结果允许输出最大 Token 数
        public const int FinalAnswerMaxOutputTokens = 800;

        // 未摘要消息达到该数量时，触发历史摘要
        public const int ConversationSummaryTriggerMessageCount = 12;

        // 最近消息保留原文，不进入历史摘要
        public const int ConversationSummaryKeepRecentCount = 6;

        // 每次最多摘要的消息数量
        public const int ConversationSummaryBatchSize = 10;

        // 摘要模型最大输出 Token
        public const int ConversationSummaryMaxOutputTokens = 800;

        // 根据文章正文回答问题时的最大输出 Token
        public const int ContentQuestionAnswerMaxOutputTokens = 1000;

        // Agent意图分类最大输出 Token
        public const int IntentRouterMaxOutputTokens = 300;

        // 直接聊天路由下回复最大输出 Token
        public const int DirectChatMaxOutputToken = 150;

        // 文章润色最大输出 Token
        public const int ContentPolishMaxOutputTokens = 2048;

        // 检查用户消息是否超过最大允许长度
        public static bool IsUserMessageTooLong(string userMessage)
        {
            return !string.IsNullOrWhiteSpace(userMessage)
                && userMessage.Length > MaxUserMessageChars;
        }
    }
}