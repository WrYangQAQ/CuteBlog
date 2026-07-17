using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentMemoryFact
    {
        // 记忆事实的类型标识（如 "ArticleSelected"、"ArticleMentioned" 等）
        public ArticleMemoryType Type { get; set; } = ArticleMemoryType.Unknown;

        // 关联的文章 ID（若有）
        public int? ArticleId { get; set; }

        // 关联的文章标题（若有）
        public string? ArticleTitle { get; set; }

        // 关联的分类名称（若有）
        public string? CategoryName { get; set; }

        // 产生该记忆的来源动作名称（如 "SearchArticlesByCategory"）
        public string? SourceAction { get; set; }

        // 记忆的摘要描述（可为简要概括或关键信息）
        public string? Summary { get; set; }
    }
}
