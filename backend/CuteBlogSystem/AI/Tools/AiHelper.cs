using CuteBlogSystem.Enum;

namespace CuteBlogSystem.AI.Tools
{
    public static class AiHelper
    {
        public static ArticleSortBy NormalizeSortBy(string? sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return ArticleSortBy.Latest;
            }

            sortBy = sortBy.Trim();

            return sortBy switch
            {
                "Latest" => ArticleSortBy.Latest,
                "MostLiked" => ArticleSortBy.MostLiked,
                "MostViewed" => ArticleSortBy.MostViewed,

                "最新" => ArticleSortBy.Latest,
                "最新发布" => ArticleSortBy.Latest,
                "时间" => ArticleSortBy.Latest,

                "点赞" => ArticleSortBy.MostLiked,
                "点赞最多" => ArticleSortBy.MostLiked,
                "点赞最高" => ArticleSortBy.MostLiked,
                "最受欢迎" => ArticleSortBy.MostLiked,

                "浏览" => ArticleSortBy.MostViewed,
                "浏览量" => ArticleSortBy.MostViewed,
                "浏览最多" => ArticleSortBy.MostViewed,
                "访问量最高" => ArticleSortBy.MostViewed,

                _ => ArticleSortBy.Latest
            };
        }
    }
}
