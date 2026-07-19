using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class UpdateArticleTitleInput
    {
        // 文章 ID（与 ArticleIdFromStep 二选一）
        public int ArticleId { get; set; }

        // 引用前面某步骤的结果作为文章 ID（与 ArticleId 二选一）
        public int ArticleIdFromStep { get; set; }

        // 新的文章标题
        public string NewTitle { get; set; } = string.Empty;
    }

    public class UpdateArticleTitleOutput : IUserReadableOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider
    {
        // 被修改的文章 ID
        public int ArticleId { get; set; }

        // 修改前的原标题
        public string OldTitle { get; set; } = string.Empty;

        // 修改后的新标题
        public string NewTitle { get; set; } = string.Empty;

        // 修改时间（UTC）
        public DateTime UpdatedAt { get; set; }

        // 生成对用户友好的可读文本摘要
        public string ToUserReadableText()
        {
            return $"文章ID为{ArticleId}的标题已成功修改，原标题为“{OldTitle}”，新标题为“{NewTitle}”。";
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
                    ArticleTitle = NewTitle,
                    SourceAction = sourceAction,
                    Summary = $"用户将文章标题从《{OldTitle}》修改为《{NewTitle}》。"
                },
                new AgentMemoryFact
                {
                    Type = ArticleMemoryType.ArticleSelected,
                    ArticleId = ArticleId,
                    ArticleTitle = NewTitle,
                    SourceAction = sourceAction,
                    Summary = $"当前选中的文章更新为《{NewTitle}》。"
                }
            };

            return facts;
        }

        public UpdateArticleTitleOutput(UpdateArticleTitleDTO dto)
        {
            ArticleId = dto.ArticleId;
            OldTitle = dto.OldTitle;
            NewTitle = dto.NewTitle;
            UpdatedAt = dto.UpdatedAt;
        }
    }
}