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

    public class GetMyArticlesOutput : IUserReadableOutput, IAgentArticleReferenceOutput
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
                $"{index + 1}. 《{article.Title}》 分类：{article.CategoryName}，点赞：{article.LikeCount}，浏览：{article.ViewCount}，ID：{article.Id}");

            return $"用户共发布 {TotalCount} 篇文章，本次返回 {Articles.Count} 篇：\n" + string.Join("\n", lines);
        }

        public int? GetPrimaryArticleId()
        {
            return Articles.FirstOrDefault()?.Id;
        }
    }
}
