using CuteBlogSystem.Enum;

namespace CuteBlogSystem.AI.Planner
{
    // 集中管理 Agent 支持的所有动作名称及相关常量，避免硬编码字符串
    public static class AgentActionRegistry
    {
        // 按分类搜索文章（主要工作流动作）
        public const string SearchArticlesByCategory = nameof(SearchArticlesByCategory);

        // 根据文章 ID 获取正文内容
        public const string GetArticleContentById = nameof(GetArticleContentById);

        // 总结文章正文
        public const string SummarizeContent = nameof(SummarizeContent);

        // 对比两篇文章
        public const string CompareContents = nameof(CompareContents);

        // 获取自己名下的所有文章
        public const string GetMyArticles = nameof(GetMyArticles);

        // 给自己名下的文章做标题修改
        public const string UpdateArticleTitle = nameof(UpdateArticleTitle);

        // 根据文章正文提出修改建议
        public const string GenerateContentRevision = nameof(GenerateContentRevision);

        // 对文章正文进行修改
        public const string UpdateArticleContent = nameof(UpdateArticleContent);

        // 删除自己名下的文章（管理员能删除所有文章）
        public const string DeleteArticle = nameof(DeleteArticle);

        // 获取所有分类（用于补救计划）
        public const string GetAllCategories = nameof(GetAllCategories);

        // 根据文章内容回答具体问题
        public const string AnswerQuestionFromContent = nameof(AnswerQuestionFromContent);

        // 解释失败并提供建议（用于补救计划）
        public const string ExplainFailureWithSuggestions = nameof(ExplainFailureWithSuggestions);

        // 所有允许执行的动作（主流程 + 补救流程）
        public static readonly HashSet<string> AllowedActions = new()
        {
            SearchArticlesByCategory,
            GetArticleContentById,
            SummarizeContent,
            CompareContents,
            GetAllCategories,
            ExplainFailureWithSuggestions,
            AnswerQuestionFromContent,
            GetMyArticles,
            UpdateArticleTitle,
            GenerateContentRevision,
            UpdateArticleContent,
            DeleteArticle
        };

        // 仅补救计划允许执行的动作集合
        public static readonly HashSet<string> AllowedRecoveryActions = new()
        {
            GetAllCategories,
            ExplainFailureWithSuggestions
        };

        // SearchArticlesByCategory 动作中 sortBy 参数允许的值
        public static readonly HashSet<string> AllowedSortTypes = new()
        {
            "Latest",
            "MostLiked",
            "MostViewed"
        };

        // 对Agent行为的风险映射字典
        private static readonly Dictionary<string, AgentActionRiskLevel> AgentActionRiskLevels = new()
        {
            [SearchArticlesByCategory] = AgentActionRiskLevel.ReadOnly,

            [GetArticleContentById] = AgentActionRiskLevel.ReadOnly,

            [SummarizeContent] = AgentActionRiskLevel.ReadOnly,

            [AnswerQuestionFromContent] = AgentActionRiskLevel.ReadOnly,

            [GetMyArticles] = AgentActionRiskLevel.ReadOnly,

            [UpdateArticleTitle] = AgentActionRiskLevel.RequireConfirmation,

            [GenerateContentRevision] = AgentActionRiskLevel.ReadOnly,

            [UpdateArticleContent] = AgentActionRiskLevel.RequireConfirmation,

            [DeleteArticle] = AgentActionRiskLevel.Forbidden,

            [CompareContents] = AgentActionRiskLevel.ReadOnly,

            [GetAllCategories] =  AgentActionRiskLevel.ReadOnly,

            [ExplainFailureWithSuggestions] = AgentActionRiskLevel.ReadOnly
        };

        // 依据风险映射字典对行为风险等级进行查询
        public static AgentActionRiskLevel GetRiskLevel(string action)
        {
            return AgentActionRiskLevels.TryGetValue(action, out var riskLevel)
                ? riskLevel
                : AgentActionRiskLevel.Forbidden;
        }

        // 获取计划中所有步骤的最高风险等级，若计划为空则返回 Forbidden
        public static AgentActionRiskLevel GetHighestRiskLevel(AgentPlan plan)
        {
            // 计划为空或无步骤时视为最高风险（Forbidden）
            if (plan?.Steps == null || plan.Steps.Count == 0)
            {
                return AgentActionRiskLevel.Forbidden;
            }

            // 取所有步骤风险等级中的最大值（枚举值最大即风险最高）
            return plan.Steps
                .Select(step => GetRiskLevel(step.Action))
                .Max();
        }
    }
}