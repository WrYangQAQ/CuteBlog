namespace CuteBlogSystem.AI.Tools
{
    public static class AiHelper
    {
        public static string NormalizeSortBy(string? sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return "Latest";
            }

            sortBy = sortBy.Trim();

            return sortBy switch
            {
                "Latest" => "Latest",
                "MostLiked" => "MostLiked",
                "MostViewed" => "MostViewed",

                "最新" => "Latest",
                "最新发布" => "Latest",
                "时间" => "Latest",

                "点赞" => "MostLiked",
                "点赞最多" => "MostLiked",
                "点赞最高" => "MostLiked",
                "最受欢迎" => "MostLiked",

                "浏览" => "MostViewed",
                "浏览量" => "MostViewed",
                "浏览最多" => "MostViewed",
                "访问量最高" => "MostViewed",

                _ => "Latest"
            };
        }
    }
}
