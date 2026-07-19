using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class UpdateArticleContentInput
    {
        // 文章 ID（与 ArticleIdFromStep 二选一）
        public int ArticleId { get; set; }

        // 引用前面某步骤的结果作为文章 ID（与 ArticleId 二选一）
        public int ArticleIdFromStep { get; set; }

        // 直接传入的新正文内容（与 NewContentFromStep 二选一）
        public string NewContent { get; set; } = string.Empty;

        // 从前面某步骤结果中提取新正文内容（与 NewContent 二选一）
        public int NewContentFromStep { get; set; }
    }

    public class UpdateArticleContentOutput : IUserReadableOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider
    {
        // 被更新内容的文章 ID
        public int ArticleId { get; set; }

        // 文章的标题
        public string Title { get; set; } = string.Empty;

        // 更新前的内容长度（字符数）
        public int OldLength { get; set; }

        // 更新后的内容长度（字符数）
        public int NewLength { get; set; }

        // 更新操作的执行时间（UTC）
        public DateTime UpdatedAt { get; set; }

        // 从数据层 UpdateArticleInformation 构造输出对象
        public UpdateArticleContentOutput(UpdateArticleInformation dto)
        {
            ArticleId = dto.ArticleId;
            Title = dto.Title;
            OldLength = dto.OldLength;
            NewLength = dto.NewLength;
            UpdatedAt = dto.UpdatedAt;
        }

        // 生成对用户友好的可读文本摘要
        public string ToUserReadableText()
        {
            return $"文章ID为{ArticleId}的《{Title}》正文已成功更新，原正文长度为{OldLength}字符，新正文长度为{NewLength}字符。";
        }

        // 获取主要涉及的文章 ID（用于记忆和引用）
        public int? GetPrimaryArticleId()
        {
            return ArticleId;
        }

        public IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction)
        {
            var facts = new List<AgentMemoryFact>
            {
                new AgentMemoryFact
                {
                    Type = ArticleMemoryType.ArticleUpdated,
                    ArticleId = ArticleId,
                    ArticleTitle = Title,
                    SourceAction = sourceAction,
                    Summary = $"用户更新了文章《{Title}》的正文内容，长度从 {OldLength} 字符变为 {NewLength} 字符。"
                },
                new AgentMemoryFact
                {
                    Type = ArticleMemoryType.ArticleSelected,
                    ArticleId = ArticleId,
                    ArticleTitle = Title,
                    SourceAction = sourceAction,
                    Summary = $"当前选中的文章为刚刚更新正文的《{Title}》。"
                }
            };

            return facts;
        }
    }
}