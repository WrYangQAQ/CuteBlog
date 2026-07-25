using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Service
{
    // 单类长期记忆的生命周期策略
    public sealed class MemoryLifecyclePolicy
    {
        // 初次创建时的有效天数
        public int InitialExpirationDays { get; init; }

        // 被检索并放入 Prompt 后续期的天数
        // null 表示普通访问不续期
        public int? AccessRenewalDays { get; init; }

        // 用户再次明确陈述相同信息时续期的天数
        public int ConfirmationRenewalDays { get; init; }

        // 每经过一天后保留的置信度比例
        public decimal DailyConfidenceRetentionRate { get; init; }
    }

    // 根据记忆分组提供统一的生命周期策略
    public static class MemoryLifecyclePolicyProvider
    {
        // 根据记忆分组类型获取对应的生命周期管理策略
        public static MemoryLifecyclePolicy GetPolicy(MemoryGroupConstants memoryGroup)
        {
            switch (memoryGroup)
            {
                // 用户偏好属于长期信息：
                // 初始有效期180天，普通检索不续期，用户再次确认时续期180天
                case MemoryGroupConstants.UserPreference:
                    return new MemoryLifecyclePolicy
                    {
                        InitialExpirationDays = 180,
                        AccessRenewalDays = null,
                        ConfirmationRenewalDays = 180,
                        DailyConfidenceRetentionRate = 0.998m
                    };

                // Agent交互习惯同样属于长期信息
                case MemoryGroupConstants.AgentBehaviour:
                    return new MemoryLifecyclePolicy
                    {
                        InitialExpirationDays = 180,
                        AccessRenewalDays = null,
                        ConfirmationRenewalDays = 180,
                        DailyConfidenceRetentionRate = 0.998m
                    };

                // 项目和学习进度变化相对较快
                case MemoryGroupConstants.ProjectLearning:
                    return new MemoryLifecyclePolicy
                    {
                        InitialExpirationDays = 60,
                        AccessRenewalDays = 60,
                        ConfirmationRenewalDays = 60,
                        DailyConfidenceRetentionRate = 0.99m
                    };

                // 博客操作习惯比普通会话上下文稳定
                case MemoryGroupConstants.BlogOperation:
                    return new MemoryLifecyclePolicy
                    {
                        InitialExpirationDays = 90,
                        AccessRenewalDays = 90,
                        ConfirmationRenewalDays = 90,
                        DailyConfidenceRetentionRate = 0.99m
                    };

                // 文章上下文属于短期信息
                case MemoryGroupConstants.ArticleContext:
                    return new MemoryLifecyclePolicy
                    {
                        InitialExpirationDays = 15,
                        AccessRenewalDays = 15,
                        ConfirmationRenewalDays = 15,
                        DailyConfidenceRetentionRate = 0.97m
                    };

                // 跨会话对话上下文也采用15天滑动过期
                case MemoryGroupConstants.ConversationContext:
                    return new MemoryLifecyclePolicy
                    {
                        InitialExpirationDays = 15,
                        AccessRenewalDays = 15,
                        ConfirmationRenewalDays = 15,
                        DailyConfidenceRetentionRate = 0.97m
                    };

                // Unknown 理论上不会被保存，这里提供防御性兜底
                default:
                    return new MemoryLifecyclePolicy
                    {
                        InitialExpirationDays = 30,
                        AccessRenewalDays = null,
                        ConfirmationRenewalDays = 30,
                        DailyConfidenceRetentionRate = 0.99m
                    };
            }
        }
    }
}