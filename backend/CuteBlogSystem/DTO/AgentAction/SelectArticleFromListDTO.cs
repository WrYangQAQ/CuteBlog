using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class SelectArticleFromListInput
    {
        // 文章列表的来源步骤编号（引用前置步骤的结果）
        public int ListFromStep { get; set; }

        // 匹配模式（如按索引、按标题、未找到等）
        public ArticleSelectionMatchMode MatchType { get; set; } = ArticleSelectionMatchMode.NotFound;

        // 若按索引匹配，则此处为序号（1-based）
        public int? Index { get; set; }

        // 若按标题/关键词匹配，则此处为用户输入的选择文本
        public string? Selection { get; set; }
    }

    public class SelectArticleFromListOutput : IUserReadableOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider
    {
        // 最终选中的文章 ID
        public int ArticleId { get; set; }

        // 选中的文章标题
        public string Title { get; set; } = string.Empty;

        // 选中的文章所属分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 匹配方式（如 ByIndex、ByTitle、NotFound 等）
        public ArticleSelectionMatchMode MatchMode { get; set; } = ArticleSelectionMatchMode.NotFound;

        // 用户传入的原始选择文本
        public string Selection { get; set; } = string.Empty;

        // 是否成功找到匹配的文章
        public bool Found => ArticleId > 0 && MatchMode != ArticleSelectionMatchMode.NotFound;

        // 生成对用户友好的可读文本
        public string ToUserReadableText()
        {
            if (!Found)
            {
                return $"没有从最近的文章列表中找到“{Selection}”对应的文章，请换成明确的序号或标题再试一次。";
            }

            return $"已选中文章：《{Title}》（ID：{ArticleId}，分类：{CategoryName}）。";
        }

        // 获取主要涉及的文章 ID（用于引用）
        public int? GetPrimaryArticleId()
        {
            return Found ? ArticleId : null;
        }

        // 生成记忆事实，记录本次选择操作
        public IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction)
        {
            if (!Found)
            {
                return Enumerable.Empty<AgentMemoryFact>();
            }

            return new List<AgentMemoryFact>
            {
                new AgentMemoryFact
                {
                    Type = ArticleMemoryType.ArticleSelected,
                    ArticleId = ArticleId,
                    ArticleTitle = Title,
                    CategoryName = CategoryName,
                    SourceAction = sourceAction,
                    Summary = $"用户从最近的文章列表中选择了《{Title}》。"
                }
            };
        }
    }
}