using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class GetMyArticlesInput
    {
        // 用户 ID
        public int UserId { get; set; }

        // 展示数量，默认 10 篇
        public int Top { get; set; } = 10;

        // 文章列表排序方式，默认按时间最新排序
        public ArticleSortBy SortBy { get; set; } = ArticleSortBy.Latest;
    }

    public class GetMyArticlesOutput : IUserReadableOutput, IAgentArticleReferenceOutput, IAgentMemoryFactProvider, IArticleListOutput
    {
        // 文章列表
        public List<ArticleSearchResultItem> Articles { get; set; } = new();

        // 文章总数
        public int TotalCount { get; set; }

        public string ToUserReadableText()
        {
            if (Articles.Count == 0)
            {
                return "当前用户还没有发布文章。";
            }

            var lines = Articles.Select((article, index) =>
                $"{index + 1}. 《{article.Title}》 ID：{article.Id}，分类：{article.CategoryName}，点赞：{article.LikeCount}，浏览：{article.ViewCount}，ID：{article.Id}");

            return $"您一共发布了 {TotalCount} 篇文章，本次返回 {Articles.Count} 篇：\n" + string.Join("\n", lines);
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
