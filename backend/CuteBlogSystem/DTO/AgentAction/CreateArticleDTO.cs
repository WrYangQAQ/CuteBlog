using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    // 发布文章动作的输入参数
    public class CreateArticleInput
    {
        // 文章标题，可由 AI 根据 Description 生成
        public string Title { get; set; } = string.Empty;

        // 文章正文，可由 AI 根据 Description 生成
        public string Content { get; set; } = string.Empty;

        // 文章摘要，可由 AI 根据正文生成
        public string Summary { get; set; } = string.Empty;

        // 分类 ID
        public int CategoryId { get; set; }

        // 从前置步骤提取分类 ID
        public int? CategoryIdFromStep { get; set; }

        // 分类名称，用于在没有分类 ID 时辅助匹配
        public string CategoryName { get; set; } = string.Empty;

        // 标签 ID 列表
        public List<int> TagIds { get; set; } = new();

        // 文章生成方向说明
        public string Description { get; set; } = string.Empty;

        // 封面临时路径，当前发布服务要求必须先上传封面
        public string CoverUrl { get; set; } = string.Empty;
    }

    // 发布文章动作的输出结果
    public class CreateArticleOutput : IUserReadableOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider
    {
        // 新文章 ID
        public int ArticleId { get; set; }

        // 新文章标题
        public string Title { get; set; } = string.Empty;

        // 分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 创建时间
        public DateTime CreatedAt { get; set; }

        // 正文长度
        public int ContentLength { get; set; }

        public string ToUserReadableText()
        {
            return $"文章《{Title}》已发布成功，分类：{CategoryName}，正文长度：{ContentLength} 字符。";
        }

        public int? GetPrimaryArticleId()
        {
            return ArticleId > 0 ? ArticleId : null;
        }

        public IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction)
        {
            if (ArticleId <= 0)
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
                    Summary = $"本次新发布了文章《{Title}》。"
                }
            };
        }
    }
}
