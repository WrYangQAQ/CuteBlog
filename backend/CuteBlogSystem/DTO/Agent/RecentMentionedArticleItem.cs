namespace CuteBlogSystem.DTO.Agent
{
    public class RecentMentionedArticleItem
    {
        // 在记忆列表中的索引位置
        public int Index { get; set; }

        // 文章 ID
        public int ArticleId { get; set; }

        // 文章标题
        public string Title { get; set; } = string.Empty;

        // 所属分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 点赞数
        public int LikeCount { get; set; }

        // 浏览数
        public int ViewCount { get; set; }

        // 记忆来源的动作名称（如 SearchArticlesByCategory）
        public string SourceAction { get; set; } = string.Empty;
    }
}
