using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class SummarizeContentInput
    {
        // 直接传入的正文内容
        public string? Content { get; set; }

        // 从前面某一步结果中提取正文内容
        public int? ContentFromStep { get; set; }
    }

    public class SummarizeContentOutput : IUserReadableOutput, IAgentMemoryFactProvider
    {
        // 生成的摘要文本
        public string Summary { get; set; } = string.Empty;

        // 原始内容的长度（字符数）
        public int OriginalContentLength { get; set; }

        // 摘要的长度（字符数）
        public int SummaryLength { get; set; }

        // 被总结的文章 ID（若有）
        public int? SourceArticleId { get; set; }

        // 被总结的文章标题（若有）
        public string? SourceArticleTitle { get; set; }

        // 被总结的文章所属分类名称（若有）
        public string? SourceCategoryName { get; set; }

        // 返回可供用户阅读的文本（即摘要本身）
        public string ToUserReadableText()
        {
            return Summary;
        }

        // 根据来源动作生成记忆事实，记录本次总结操作
        public IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction)
        {
            var facts = new List<AgentMemoryFact>
            {
                new AgentMemoryFact
                {
                    Type = ArticleMemoryType.ArticleSummarized,
                    ArticleId = SourceArticleId,
                    ArticleTitle = SourceArticleTitle,
                    CategoryName = SourceCategoryName,
                    SourceAction = sourceAction,
                    Summary = SourceArticleId.HasValue
                        ? $"用户总结了文章《{SourceArticleTitle}》。"
                        : "用户总结了一段直接提供的内容。"
                }
            };

            return facts;
        }
    }
}
