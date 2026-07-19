using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class GenerateContentRevisionInput
    {
        // 直接传入的原文内容
        public string? OriginalContent { get; set; }

        // 从前面某一步结果中提取原文内容
        public int? ContentFromStep { get; set; }

        // 修改 / 润色 / 扩写 / 改写指令
        public string Instruction { get; set; } = string.Empty;
    }

    public class GenerateContentRevisionOutput : IUserReadableOutput, IAgentContentOutput, IAgentMemoryFactProvider
    {
        // 修改指令
        public string Instruction { get; set; } = string.Empty;

        // 原文长度
        public int OriginalContentLength { get; set; }

        // 修订后的完整正文
        public string RevisedContent { get; set; } = string.Empty;

        // 修订后正文长度
        public int RevisedContentLength { get; set; }

        // 修订文章的ID
        public int? SourceArticleId { get; set; }

        // 修订文章的标题
        public string? SourceArticleTitle { get; set; }

        // 修订文章的分类
        public string? SourceCategoryName { get; set; }

        public string ToUserReadableText()
        {
            return RevisedContent;
        }

        public string GetContentText()
        {
            return RevisedContent;
        }

        public IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction)
        {
            var facts = new List<AgentMemoryFact>();

            if (!SourceArticleId.HasValue || string.IsNullOrWhiteSpace(SourceArticleTitle))
            {
                return facts;
            }

            facts.Add(new AgentMemoryFact
            {
                Type = ArticleMemoryType.ArticleMentioned,
                ArticleId = SourceArticleId,
                ArticleTitle = SourceArticleTitle,
                CategoryName = SourceCategoryName,
                SourceAction = sourceAction,
                Summary = $"用户基于文章《{SourceArticleTitle}》生成了一份修订内容，但尚不代表已写入文章。"
            });

            return facts;
        }
    }
}