using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    // 根据标签查询文章动作的输入参数
    public class SearchArticlesByTagInput
    {
        // 标签 ID
        public int TagId { get; set; }

        // 从前置步骤提取标签 ID
        public int? TagIdFromStep { get; set; }

        // 排序方式
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.Latest;

        // 返回数量
        public int Top { get; set; } = 10;
    }

    // 根据标签查询文章动作的输出结果
    public class SearchArticlesByTagOutput : IUserReadableOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider, IArticleListOutput
    {
        // 标签 ID
        public int TagId { get; set; }

        // 标签名称
        public string TagName { get; set; } = string.Empty;

        // 排序方式
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.Latest;

        // 命中的文章列表
        public List<ArticleSearchResultItem> Articles { get; set; } = new();

        // 命中文章数量
        public int TotalCount { get; set; }

        public string ToUserReadableText()
        {
            if (Articles.Count == 0)
            {
                return $"标签「{TagName}」下没有找到文章。";
            }

            var articleLines = Articles.Select((article, index) =>
                $"{index + 1}. {article.Title}，ID：{article.Id}，分类：{article.CategoryName}，点赞数：{article.LikeCount}，浏览量：{article.ViewCount}");

            return $"""
            标签：{TagName}
            排序方式：{SortBy}
            文章数量：{TotalCount}

            {string.Join("\n", articleLines)}
            """;
        }

        public int? GetPrimaryArticleId()
        {
            return Articles.Count == 1 ? Articles[0].Id : null;
        }

        public IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction)
        {
            var facts = Articles.Select(article => new AgentMemoryFact
            {
                Type = ArticleMemoryType.ArticleMentioned,
                ArticleId = article.Id,
                ArticleTitle = article.Title,
                CategoryName = article.CategoryName,
                SourceAction = sourceAction,
                Summary = $"标签「{TagName}」的文章列表中提到了《{article.Title}》。"
            }).ToList();

            if (Articles.Count == 1)
            {
                var article = Articles[0];
                facts.Add(new AgentMemoryFact
                {
                    Type = ArticleMemoryType.ArticleSelected,
                    ArticleId = article.Id,
                    ArticleTitle = article.Title,
                    CategoryName = article.CategoryName,
                    SourceAction = sourceAction,
                    Summary = $"标签「{TagName}」下只有一篇文章，自动选中《{article.Title}》。"
                });
            }

            return facts;
        }
    }
}
