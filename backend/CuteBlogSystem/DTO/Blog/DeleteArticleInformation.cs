namespace CuteBlogSystem.DTO.Blog
{
    public class DeleteArticleInformation
    {
        // 被删除的文章 ID
        public int ArticleId { get; set; }

        // 被删除的文章标题（用于用户友好展示）
        public string Title { get; set; } = string.Empty;

        // 删除操作是否成功
        public bool Deleted { get; set; }

        // 删除操作的结果消息（成功或失败的具体说明）
        public string Message { get; set; } = string.Empty;
    }
}
