using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    // 多维度搜索文章动作的输入参数
    public class SearchArticlesByKeywordInput
    {
        // 查询关键词
        public string QueryText { get; set; } = string.Empty;

        // 搜索范围：ByTitle、ByContent、ByTag、ByCategory、ByAll
        public ArticleSearchScope SearchScope { get; set; } = ArticleSearchScope.ByAll;

        // 文章范围：All 表示全站，My 表示仅查询当前用户文章
        public ArticleScope ArticleScope { get; set; } = ArticleScope.All;

        // 排序方式
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.Latest;

        // 返回数量
        public int Top { get; set; } = 10;
    }

    // 多维度搜索文章动作的输出结果
    public class SearchArticlesByKeywordOutput : IUserReadableOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider, IArticleListOutput
    {
        // 查询关键词
        public string QueryText { get; set; } = string.Empty;

        // 实际搜索范围
        public ArticleSearchScope SearchScope { get; set; } = ArticleSearchScope.ByAll;

        // 实际文章范围
        public ArticleScope ArticleScope { get; set; } = ArticleScope.All;

        // 实际排序方式
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.Latest;

        // 搜索命中的文章列表
        public List<ArticleSearchResultItem> Articles { get; set; } = new();

        // 搜索命中的文章数量
        public int TotalCount { get; set; }

        public string ToUserReadableText()
        {
            if (Articles.Count == 0)
            {
                return $"没有找到与“{QueryText}”相关的文章。";
            }

            var articleLines = Articles.Select((article, index) =>
                $"{index + 1}. {article.Title}，ID：{article.Id}，分类：{article.CategoryName}，点赞数：{article.LikeCount}，浏览量：{article.ViewCount}");

            return $"""
            查询关键词：{QueryText}
            搜索范围：{SearchScope}
            文章范围：{ArticleScope}
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
                Summary = $"关键词搜索结果中提到了《{article.Title}》。"
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
                    Summary = $"关键词搜索结果只有一篇文章，自动选中《{article.Title}》。"
                });
            }

            return facts;
        }
    }
}
