using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class CompareContentsInput
    {
        // 第一篇文章正文的来源步骤编号
        public int ContentFromStepA { get; set; }

        // 第二篇文章正文的来源步骤编号
        public int ContentFromStepB { get; set; }

        // 用户指定的比较重点方向（可选）
        public string? CompareFocus { get; set; }

        // 第一篇文章的正文文本
        public string ContentA { get; set; } = string.Empty;

        // 第二篇文章的正文文本
        public string ContentB { get; set; } = string.Empty;
    }

    public class CompareContentsOutput : IUserReadableOutput, IAgentMemoryFactProvider
    {
        // AI 生成的对比分析结果文本
        public string Comparison { get; set; } = string.Empty;

        // 第一篇文章的正文长度（字符数）
        public int ContentALength { get; set; }

        // 第二篇文章的正文长度（字符数）
        public int ContentBLength { get; set; }

        // 对比分析结果的长度（字符数）
        public int ComparisonLength { get; set; }

        // 实际使用的比较重点方向（若用户指定）
        public string? CompareFocus { get; set; }


        // 第一篇文章的 ID（若有）
        public int? ArticleAId { get; set; }

        // 第一篇文章的标题（若有）
        public string? ArticleATitle { get; set; }

        // 第一篇文章的分类名称（若有）
        public string? ArticleACategoryName { get; set; }

        // 第二篇文章的 ID（若有）
        public int? ArticleBId { get; set; }

        // 第二篇文章的标题（若有）
        public string? ArticleBTitle { get; set; }

        // 第二篇文章的分类名称（若有）
        public string? ArticleBCategoryName { get; set; }

        // 返回可供用户阅读的文本（即对比分析结果）
        public string ToUserReadableText()
        {
            return Comparison;
        }

        // 根据来源动作生成记忆事实，记录本次对比分析涉及的两篇文章
        public IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction)
        {
            var facts = new List<AgentMemoryFact>();

            // 为第一篇文章添加提及事实
            AddMentionFact(
                facts,
                ArticleAId,
                ArticleATitle,
                ArticleACategoryName,
                sourceAction,
                "对比分析中涉及第一篇文章");

            // 为第二篇文章添加提及事实
            AddMentionFact(
                facts,
                ArticleBId,
                ArticleBTitle,
                ArticleBCategoryName,
                sourceAction,
                "对比分析中涉及第二篇文章");

            return facts;
        }

        // 辅助方法：为单篇文章添加“提及”类型的记忆事实，自动去重
        private void AddMentionFact(
            List<AgentMemoryFact> facts,
            int? articleId,
            string? articleTitle,
            string? categoryName,
            string sourceAction,
            string summaryPrefix)
        {
            // 缺少有效文章 ID 或标题则跳过
            if (!articleId.HasValue || string.IsNullOrWhiteSpace(articleTitle))
            {
                return;
            }

            // 避免同一篇文章重复添加记忆事实
            if (facts.Any(f => f.ArticleId == articleId))
            {
                return;
            }

            facts.Add(new AgentMemoryFact
            {
                Type = ArticleMemoryType.ArticleMentioned,
                ArticleId = articleId,
                ArticleTitle = articleTitle,
                CategoryName = categoryName,
                SourceAction = sourceAction,
                Summary = $"{summaryPrefix}：《{articleTitle}》"
            });
        }
    }
}