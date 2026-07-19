using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class GetArticleContentByIdInput
    {
        // 文章 ID（与 ArticleIdFromStep 二选一）
        public int ArticleId { get; set; }

        // 引用前面某步骤的结果作为文章 ID（与 ArticleId 二选一）
        public int? ArticleIdFromStep { get; set; }
    }

    public class GetArticleContentByIdOutput : IUserReadableOutput, IAgentContentOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider
    {
        // 文章 ID
        public int ArticleId { get; set; }

        // 文章标题
        public string Title { get; set; } = string.Empty;

        // 文章正文内容
        public string Content { get; set; } = string.Empty;

        // 作者名称
        public string AuthorName { get; set; } = string.Empty;

        // 所属分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 关联的标签名称列表
        public List<string> TagNames { get; set; } = new();

        // 文章创建时间
        public DateTime CreatedAt { get; set; }

        // 正文长度（字符数）
        public int ContentLength { get; set; }

        // 从 DisplayArticleDTO 构造 GetArticleContentByIdOutput
        public GetArticleContentByIdOutput(int articleId, DisplayArticleDTO dto)
        {
            ArticleId = articleId;
            Title = dto.Title;
            Content = dto.Content;
            AuthorName = dto.AuthorName;
            CategoryName = dto.CategoryName;
            TagNames = dto.TagNames ?? new List<string>();
            CreatedAt = dto.CreatedAt;
            ContentLength = dto.Content?.Length ?? 0;
        }

        public string ToUserReadableText()
        {
            return $"""
            标题：{Title}
            分类：{CategoryName}
            作者：{AuthorName}

            正文：
            {Content}
            """;
        }

        public string GetContentText()
        {
            return Content;
        }

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
                    Type = ArticleMemoryType.ArticleSelected,
                    ArticleId = ArticleId,
                    ArticleTitle = Title,
                    CategoryName = CategoryName,
                    SourceAction = sourceAction,
                    Summary = $"用户查看了文章《{Title}》的正文内容。"
                }
            };

            return facts;
        }
    }
}
