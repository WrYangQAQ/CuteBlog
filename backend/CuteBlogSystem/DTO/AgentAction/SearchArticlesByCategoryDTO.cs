using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    // 搜索文章动作的输入参数
    public class SearchArticlesByCategoryInput
    {
        // 文章分类名称（必填）
        public string CategoryName { get; set; } = string.Empty;

        // 排序方式：MostLiked（按点赞）、MostViewed（按浏览）、Latest（按最新）等
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.MostLiked;

        // 返回的最大文章数量（默认5，建议1~10）
        public int Top { get; set; } = 5;
    }

    // 搜索文章动作的输出结果
    public class SearchArticlesByCategoryOutput : IUserReadableOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider, IArticleListOutput
    {
        // 匹配的文章列表（可能为空）
        public List<ArticleSearchResultItem> Articles { get; set; } = new();

        // 实际查询的分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 实际使用的排序方式（默认时间排序）
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.Latest;

        // 该分类下的总文章数（用于分页或提示）
        public int TotalCount { get; set; }

        public string ToUserReadableText()
        {
            if (Articles.Count == 0)
            {
                return $"分类「{CategoryName}」下没有找到文章。";
            }

            var articleLines = Articles.Select((article, index) =>
                $"{index + 1}. {article.Title}，分类：{article.CategoryName}，点赞数：{article.LikeCount}，浏览量：{article.ViewCount}");

            return $"""
            分类：{CategoryName}
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
            var facts = new List<AgentMemoryFact>();

            foreach (var article in Articles)
            {
                facts.Add(new AgentMemoryFact
                {
                    Type = ArticleMemoryType.ArticleMentioned,
                    ArticleId = article.Id,
                    ArticleTitle = article.Title,
                    CategoryName = article.CategoryName,
                    SourceAction = sourceAction,
                    Summary = $"本次文章列表中提到了《{article.Title}》。"
                });
            }

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
                    Summary = $"本次结果只有一篇文章，自动选中《{article.Title}》。"
                });
            }

            return facts;
        }
    }

    
}
