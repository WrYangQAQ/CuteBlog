using CuteBlogSystem.DTO.Blog;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class DeleteArticleInput
    {
        // 要删除的文章 ID（与 ArticleIdFromStep 二选一）
        public int ArticleId { get; set; }

        // 引用前面某步骤的结果作为文章 ID（与 ArticleId 二选一）
        public int ArticleIdFromStep { get; set; }
    }

    public class DeleteArticleOutput : IUserReadableOutput, IAgentArticleReferenceOutput
    {
        // 被删除的文章 ID
        public int ArticleId { get; set; }

        // 被删除的文章标题（用于用户友好展示）
        public string Title { get; set; } = string.Empty;

        // 删除操作是否成功
        public bool Deleted { get; set; }

        // 删除操作的结果消息（成功或失败的具体说明）
        public string Message { get; set; } = string.Empty;

        public DeleteArticleOutput(DeleteArticleInformation dto)
        {
            ArticleId = dto.ArticleId;
            Title = dto.Title;
            Deleted = dto.Deleted;
            Message = dto.Message;
        }

        // 获取主要涉及的文章 ID（用于记忆和引用，若无有效 ID 则返回 null）
        public int? GetPrimaryArticleId()
        {
            return ArticleId > 0 ? ArticleId : null;
        }

        // 生成对用户友好的可读文本摘要
        public string ToUserReadableText()
        {
            // 删除失败时返回错误消息或默认提示
            if (!Deleted)
            {
                return string.IsNullOrWhiteSpace(Message)
                    ? "文章删除失败。"
                    : Message;
            }

            // 删除成功时构造包含文章标题（或ID）的成功消息
            var titleText = string.IsNullOrWhiteSpace(Title)
                ? $"ID 为 {ArticleId} 的文章"
                : $"《{Title}》";

            return $"已删除文章 {titleText}。";
        }
    }
}